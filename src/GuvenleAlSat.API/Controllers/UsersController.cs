using System.Security.Claims;
using GuvenleAlSat.DataAccess.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GuvenleAlSat.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized(new { success = false, message = "Oturum doğrulanamadı." });

        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null)
            return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        return Ok(new
        {
            success = true,
            data = new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.StoreName,
                userType = user.UserType.ToString(),
                user.IsNviVerified
            }
        });
    }
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized(new { success = false, message = "Oturum doğrulanamadı." });

        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null)
            return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        // Identity'nin yerleşik ChangePasswordAsync metodu eski şifreyi otomatik doğrular:
        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (result.Succeeded)
        {
            return Ok(new { success = true, message = "Şifreniz başarıyla güncellendi." });
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return BadRequest(new { success = false, message = errors });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value
                        ?? User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            user.PhoneNumber = dto.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            user.Email = dto.Email;
            user.UserName = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.StoreName))
            user.StoreName = dto.StoreName;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
            return Ok(new { success = true, message = "Profil bilgileri güncellendi." });

        return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
    }
}

public class UpdateProfileDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? StoreName { get; set; }
}