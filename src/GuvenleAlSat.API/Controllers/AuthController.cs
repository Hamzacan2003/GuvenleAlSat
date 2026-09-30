using GuvenleAlSat.Business.Abstract;
using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.DataAccess.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GuvenleAlSat.API.Controllers;

public class VerifyEmailDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordWithOtpDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordWithOldDto
{
    public string Email { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public AuthController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _authService = authService;
        _userManager = userManager;
        _emailService = emailService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterAsync(dto, ipAddress);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(dto, ipAddress);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { success = false, message = "E-posta veya doğrulama kodu boş olamaz." });

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user == null)
            return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        if (user.EmailConfirmed)
            return Ok(new { success = true, message = "E-posta adresiniz zaten doğrulanmış." });

        if (user.EmailVerificationCode != dto.Code.Trim())
            return BadRequest(new { success = false, message = "Girdiğiniz doğrulama kodu hatalı." });

        if (user.EmailVerificationCodeExpiresAt.HasValue && user.EmailVerificationCodeExpiresAt.Value < DateTime.UtcNow)
            return BadRequest(new { success = false, message = "Doğrulama kodunun süresi dolmuş. Lütfen yeni kod isteyiniz." });

        user.EmailConfirmed = true;
        user.EmailVerificationCode = null;
        user.EmailVerificationCodeExpiresAt = null;
        await _userManager.UpdateAsync(user);

        return Ok(new { success = true, message = "E-posta adresiniz başarıyla doğrulandı! Giriş yapabilirsiniz." });
    }

    // 1. ŞİFRE SIFIRLAMA KODU (OTP) GÖNDERME
    [HttpPost("forgot-password-code")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordCode([FromBody] ForgotPasswordRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { success = false, message = "E-posta adresi boş bırakılamaz." });

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user == null)
        {
            // Kullanıcı tespit edilmesin diye yine de başarılı mesajı dönülür
            return Ok(new { success = true, message = "Eğer e-posta sistemde kayıtlı ise doğrulama kodu gönderilmiştir." });
        }

        var code = new Random().Next(100000, 999999).ToString();
        user.EmailVerificationCode = code;
        user.EmailVerificationCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
        await _userManager.UpdateAsync(user);

        _ = Task.Run(async () =>
        {
            try
            {
                var html = $@"
                    <div style=""font-family: Arial, sans-serif; max-width: 500px; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                        <div style=""background-color: #ffe100; padding: 8px 12px; font-weight: 900; font-size: 20px; display: inline-block;"">sahibinden.com</div>
                        <h3 style=""color: #333; margin-top: 15px;"">Şifre Sıfırlama Kodu</h3>
                        <p style=""color: #555;"">Hesabınızın şifresini yenilemek ve blokeyi kaldırmak için gereken güvenlik kodunuz:</p>
                        <div style=""background-color: #f3f7fc; color: #0055b8; font-size: 28px; font-weight: bold; letter-spacing: 6px; padding: 15px; text-align: center; border-radius: 6px; margin: 15px 0;"">
                            {code}
                        </div>
                        <p style=""color: #888; font-size: 11px;"">Bu kod 15 dakika boyunca geçerlidir. Talebi siz yapmadıysanız bu e-postayı dikkate almayınız.</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email!, "sahibinden.com - Şifre Sıfırlama Kodunuz: " + code, html);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[E-POSTA HATASI]: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[ŞİFRE SIFIRLAMA KODU]: {code} -> {user.Email}");
            }
        });

        return Ok(new { success = true, message = "Doğrulama kodu e-posta adresinize gönderildi." });
    }

    // 2. KOD İLE ŞİFREYİ YENİLEME VE BLOKEYİ KALDIRMA
    [HttpPost("reset-password-with-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { success = false, message = "Lütfen tüm alanları eksiksiz doldurunuz." });

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user == null)
            return BadRequest(new { success = false, message = "Girdiğiniz kod veya e-posta hatalı." });

        if (user.EmailVerificationCode != dto.Code.Trim())
            return BadRequest(new { success = false, message = "Doğrulama kodu hatalıdır." });

        if (user.EmailVerificationCodeExpiresAt.HasValue && user.EmailVerificationCodeExpiresAt.Value < DateTime.UtcNow)
            return BadRequest(new { success = false, message = "Doğrulama kodunun süresi dolmuş. Lütfen yeni kod isteyiniz." });

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);

        if (!result.Succeeded)
        {
            var err = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { success = false, message = err });
        }

        // Blokeyi kaldır, sayacı sıfırla, kodu temizle
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        user.EmailVerificationCode = null;
        user.EmailVerificationCodeExpiresAt = null;
        await _userManager.UpdateAsync(user);

        return Ok(new { success = true, message = "Şifreniz başarıyla yenilendi ve hesabınızın kilidi açıldı! Şimdi giriş yapabilirsiniz." });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.RefreshToken))
            return BadRequest(new { success = false, message = "Refresh token boş olamaz." });

        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken, ipAddress);
        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenDto dto)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RevokeTokenAsync(dto.RefreshToken, ipAddress);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"]!;

        return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
    }
}