using GuvenleAlSat.DataAccess.Enums;

namespace GuvenleAlSat.Business.DTOs;

public class RegisterDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserType UserType { get; set; } = UserType.Individual;

    public string? NationalIdNumber { get; set; }
    public int? BirthYear { get; set; }

    public string? StoreName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}