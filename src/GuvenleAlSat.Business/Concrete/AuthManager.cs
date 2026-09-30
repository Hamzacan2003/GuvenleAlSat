using System.Text.RegularExpressions;
using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;
using GuvenleAlSat.Core.Utilities.Security.Jwt;
using GuvenleAlSat.DataAccess.Concrete.EntityFramework.Contexts;
using GuvenleAlSat.DataAccess.Entities.Subscriptions;
using GuvenleAlSat.DataAccess.Entities.Users;
using GuvenleAlSat.DataAccess.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuvenleAlSat.Business.Concrete;

public class AuthManager : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenHelper _tokenHelper;
    private readonly AppDbContext _context;
    private readonly INviVerificationService _nviService;
    private readonly IEmailService _emailService;

    public AuthManager(
        UserManager<ApplicationUser> userManager,
        ITokenHelper tokenHelper,
        AppDbContext context,
        INviVerificationService nviService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _tokenHelper = tokenHelper;
        _context = context;
        _nviService = nviService;
        _emailService = emailService;
    }

    private static string ToTurkishUpper(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Trim()
            .Replace("i", "İ")
            .Replace("ı", "I")
            .Replace("ç", "Ç")
            .Replace("ğ", "Ğ")
            .Replace("ö", "Ö")
            .Replace("ş", "Ş")
            .Replace("ü", "Ü")
            .ToUpperInvariant();
    }

    private static string ToSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var clean = text.Trim().ToLowerInvariant()
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c")
            .Replace(" ", "-");
        return Regex.Replace(clean, @"[^a-z0-9\-]", "");
    }

    public async Task<IDataResult<AccessToken>> RegisterAsync(RegisterDto dto, string ipAddress)
    {
        var email = dto.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(email);

        // 1. Kullanıcı varsa kontrol et
        if (existingUser != null)
        {
            if (existingUser.EmailConfirmed)
            {
                return new ErrorDataResult<AccessToken>("Bu e-posta adresiyle zaten aktif bir hesap kayıtlı.");
            }

            // Henüz onaylanmamış eski/süresi geçmiş kaydı ve bağlı tokenları temizle:
            var oldTokens = await _context.RefreshTokens.Where(r => r.UserId == existingUser.Id).ToListAsync();
            _context.RefreshTokens.RemoveRange(oldTokens);
            await _userManager.DeleteAsync(existingUser);
            await _context.SaveChangesAsync();
        }

        var cleanFirstName = Regex.Replace(dto.FirstName?.Trim() ?? string.Empty, @"\s+", " ");
        var cleanLastName = Regex.Replace(dto.LastName?.Trim() ?? string.Empty, @"\s+", " ");

        var normalizedFirstName = ToTurkishUpper(cleanFirstName);
        var normalizedLastName = ToTurkishUpper(cleanLastName);
        var nationalId = (dto.NationalIdNumber ?? string.Empty).Trim();

        // 2. NVİ Kimlik Doğrulaması (Hata varsa kayıt durdurulur)
        if (!string.IsNullOrWhiteSpace(nationalId) && dto.BirthYear.HasValue)
        {
            var nviResult = await _nviService.VerifyAsync(
                nationalId,
                normalizedFirstName,
                normalizedLastName,
                dto.BirthYear.Value
            );

            if (nviResult == null || !nviResult.Success)
            {
                return new ErrorDataResult<AccessToken>(
                    nviResult?.Message ?? "Kimlik bilgileri doğrulanamadı! Lütfen T.C. Kimlik No, Ad, Soyad ve Doğum Yılınızı kontrol ediniz."
                );
            }
        }
        else
        {
            return new ErrorDataResult<AccessToken>("T.C. Kimlik Numarası ve Doğum Yılı zorunludur.");
        }

        // 3. 6 Haneli Doğrulama Kodu Üret (3 Dakika Geçerli)
        var verificationCode = new Random().Next(100000, 999999).ToString();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            UserType = dto.UserType,
            NationalIdNumber = nationalId,
            BirthYear = dto.BirthYear,
            IsNviVerified = true,
            NviVerifiedAt = DateTime.UtcNow,
            EmailConfirmed = false,
            EmailVerificationCode = verificationCode,
            EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(3), // 3 DAKİKA
            StoreName = dto.UserType == UserType.Corporate ? dto.StoreName?.Trim() : null,
            StoreSlug = (dto.UserType == UserType.Corporate && !string.IsNullOrWhiteSpace(dto.StoreName))
                ? ToSlug(dto.StoreName)
                : null,
            TaxNumber = dto.TaxNumber?.Trim(),
            TaxOffice = dto.TaxOffice?.Trim()
        };

        // 4. Identity ile Kullanıcı Oluşturma
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ErrorDataResult<AccessToken>($"Kayıt başarısız: {errors}");
        }

        // 5. Varsayılan Üyelik Paketi
        try
        {
            var defaultPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.TargetUserType == dto.UserType && p.IsActive);

            if (defaultPlan != null)
            {
                _context.UserSubscriptions.Add(new UserSubscription
                {
                    UserId = user.Id,
                    PlanId = defaultPlan.Id,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Subscription Plan Uyarısı]: {ex.Message}");
        }

        // 6. Token Üretimi ve Refresh Token Kaydı
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenHelper.CreateToken(
            user.Id,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            user.UserType.ToString(),
            user.IsNviVerified,
            roles
        );

        SaveRefreshToken(user.Id, token.RefreshToken, token.RefreshTokenExpiration, ipAddress);
        await _context.SaveChangesAsync();

        // 7. E-posta ile Doğrulama Kodu Gönderimi (3 Dakika Süreli Şablon)
        _ = Task.Run(async () =>
        {
            try
            {
                var htmlBody = $@"
                    <div style=""font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 8px;"">
                        <div style=""background-color: #ffe100; padding: 8px 16px; border-radius: 4px; display: inline-block; font-weight: 900; font-size: 22px; color: #111;"">
                            sahibinden.com
                        </div>
                        <h2 style=""color: #1e293b; margin-top: 24px; font-size: 18px;"">E-posta Doğrulama Kodu</h2>
                        <p style=""color: #475569; font-size: 14px; line-height: 1.5;"">
                            Sayın <strong>{user.FirstName} {user.LastName}</strong>,
                        </p>
                        <p style=""color: #475569; font-size: 14px; line-height: 1.5;"">
                            Hesabınızı aktifleştirmek için aşağıdaki 6 haneli güvenlik kodunu kullanınız:
                        </p>
                        <div style=""background-color: #f1f5f9; padding: 16px; text-align: center; border-radius: 6px; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #0055b8; margin: 24px 0;"">
                            {verificationCode}
                        </div>
                        <p style=""color: #dc2626; font-size: 13px; font-weight: bold; margin-top: 16px;"">
                            * Bu kod 3 dakika boyunca geçerlidir. Süre bitiminde kod geçersiz olacaktır.
                        </p>
                    </div>";

                await _emailService.SendEmailAsync(
                    user.Email!,
                    $"sahibinden.com - Doğrulama Kodunuz: {verificationCode}",
                    htmlBody
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[E-POSTA GÖNDERME HATASI]: {ex.Message}");
                Console.WriteLine($"[TEST DOĞRULAMA KODU]: {verificationCode} -> {user.Email}");
            }
        });

        return new SuccessDataResult<AccessToken>(token, "Hesap oluşturuldu. Lütfen 3 dakika içinde e-posta adresinize gelen doğrulama kodunu giriniz.");
    }

    public async Task<IDataResult<AccessToken>> LoginAsync(LoginDto dto, string ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user == null || !user.IsActive)
            return new ErrorDataResult<AccessToken>("Geçersiz e-posta veya şifre.");

        if (await _userManager.IsLockedOutAsync(user))
        {
            return new ErrorDataResult<AccessToken>(
                "Hesabınız 3 kez hatalı parola girildiği için bloke edilmiştir! Güvenliğiniz için lütfen 'Şifremi Unuttum' seçeneğini kullanarak şifrenizi yenileyiniz."
            );
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new ErrorDataResult<AccessToken>(
                    "Hesabınız 3 kez hatalı parola girildiği için bloke edilmiştir! Güvenliğiniz için lütfen 'Şifremi Unuttum' seçeneğini kullanarak şifrenizi yenileyiniz."
                );
            }

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            int remaining = 3 - failedCount;
            return new ErrorDataResult<AccessToken>($"Geçersiz e-posta veya şifre. Kalan deneme hakkınız: {remaining}");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenHelper.CreateToken(
            user.Id,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            user.UserType.ToString(),
            user.IsNviVerified,
            roles
        );

        SaveRefreshToken(user.Id, token.RefreshToken, token.RefreshTokenExpiration, ipAddress);
        await _context.SaveChangesAsync();

        return new SuccessDataResult<AccessToken>(token, "Giriş başarılı.");
    }

    public async Task<IDataResult<AccessToken>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var existingToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (existingToken == null)
            return new ErrorDataResult<AccessToken>("Geçersiz Refresh Token.");

        if (existingToken.IsRevoked || existingToken.IsExpired)
        {
            var allUserTokens = await _context.RefreshTokens
                .Where(r => r.UserId == existingToken.UserId && !r.IsRevoked)
                .ToListAsync();

            foreach (var t in allUserTokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            return new ErrorDataResult<AccessToken>("Güvenlik ihlali tespit edildi! Lütfen tekrar giriş yapın.");
        }

        var user = existingToken.User;
        var roles = await _userManager.GetRolesAsync(user);
        var newToken = _tokenHelper.CreateToken(
            user.Id,
            user.Email!,
            $"{user.FirstName} {user.LastName}",
            user.UserType.ToString(),
            user.IsNviVerified,
            roles
        );

        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByToken = newToken.RefreshToken;

        SaveRefreshToken(user.Id, newToken.RefreshToken, newToken.RefreshTokenExpiration, ipAddress);
        await _context.SaveChangesAsync();

        return new SuccessDataResult<AccessToken>(newToken, "Token başarıyla yenilendi.");
    }

    public async Task<IResult> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (token == null || !token.IsActive)
            return new ErrorResult("Geçersiz veya süresi dolmuş token.");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SuccessResult("Oturum kapatıldı.");
    }

    private void SaveRefreshToken(Guid userId, string token, DateTime expiresAt, string ipAddress)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = ipAddress
        });
    }
}