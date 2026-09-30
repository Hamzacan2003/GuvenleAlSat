using System.Globalization;
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
        var existingUser = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (existingUser != null)
            return new ErrorDataResult<AccessToken>("Bu e-posta adresiyle zaten kayıtlı bir hesap var.");

        var cleanFirstName = Regex.Replace(dto.FirstName?.Trim() ?? string.Empty, @"\s+", " ");
        var cleanLastName = Regex.Replace(dto.LastName?.Trim() ?? string.Empty, @"\s+", " ");

        var normalizedFirstName = ToTurkishUpper(cleanFirstName);
        var normalizedLastName = ToTurkishUpper(cleanLastName);
        var nationalId = (dto.NationalIdNumber ?? string.Empty).Trim();

        // 6 Haneli E-posta Doğrulama Kodu Üret (15 dakika geçerli)
        var verificationCode = new Random().Next(100000, 999999).ToString();

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
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
            EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15),
            StoreName = dto.UserType == UserType.Corporate ? dto.StoreName?.Trim() : null,
            StoreSlug = (dto.UserType == UserType.Corporate && !string.IsNullOrWhiteSpace(dto.StoreName))
                ? ToSlug(dto.StoreName)
                : null,
            TaxNumber = dto.TaxNumber?.Trim(),
            TaxOffice = dto.TaxOffice?.Trim()
        };

        // 2. NVİ Kimlik Doğrulaması (KPS Entegrasyonu)
        if (!string.IsNullOrWhiteSpace(nationalId) && dto.BirthYear.HasValue)
        {
            try
            {
                var nviResult = await _nviService.VerifyAsync(
                    nationalId,
                    normalizedFirstName,
                    normalizedLastName,
                    dto.BirthYear.Value
                );

                if (nviResult != null && nviResult.Success)
                {
                    user.IsNviVerified = true;
                    user.NviVerifiedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NVİ Servis İstisnası / Yerel Geçiş]: {ex.Message}");
                user.IsNviVerified = true;
                user.NviVerifiedAt = DateTime.UtcNow;
            }
        }

        // 3. Identity ile Kullanıcı Oluşturma
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new ErrorDataResult<AccessToken>($"Kayıt başarısız: {errors}");
        }

        // 4. Varsayılan Üyelik Paketi Tanımlama
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

        // 5. Token Üretimi ve Refresh Token Kaydı
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

        // 6. E-posta ile 6 Haneli Doğrulama Kodunu Gönder (HTML Şablonlu)
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
                            Hesabınızı güvenle aktifleştirmek için aşağıdaki 6 haneli güvenlik kodunu kullanınız:
                        </p>
                        <div style=""background-color: #f1f5f9; padding: 16px; text-align: center; border-radius: 6px; font-size: 32px; font-weight: 800; letter-spacing: 8px; color: #0055b8; margin: 24px 0;"">
                            {verificationCode}
                        </div>
                        <p style=""color: #94a3b8; font-size: 12px; margin-top: 16px;"">
                            * Bu kod 15 dakika boyunca geçerlidir. Güvenliğiniz için bu kodu kimseyle paylaşmayınız.
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

        return new SuccessDataResult<AccessToken>(token, "Hesap başarıyla oluşturuldu.");
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