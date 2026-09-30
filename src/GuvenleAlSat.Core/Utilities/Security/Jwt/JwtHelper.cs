using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GuvenleAlSat.Core.Utilities.Security.Jwt;

public class JwtHelper : ITokenHelper
{
    private readonly TokenOptions _tokenOptions;

    public JwtHelper(IConfiguration configuration)
    {
        var section = configuration.GetSection("TokenOptions");

        _tokenOptions = new TokenOptions
        {
            Audience = section["Audience"] ?? string.Empty,
            Issuer = section["Issuer"] ?? string.Empty,
            AccessTokenExpirationMinutes = int.TryParse(section["AccessTokenExpirationMinutes"], out var aExp) ? aExp : 15,
            RefreshTokenExpirationDays = int.TryParse(section["RefreshTokenExpirationDays"], out var rExp) ? rExp : 30,
            SecurityKey = section["SecurityKey"] ?? throw new InvalidOperationException("SecurityKey konfigürasyonda bulunamadı.")
        };
    }

    public AccessToken CreateToken(Guid userId, string email, string fullName, string userType, bool isNviVerified, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
            new("UserType", userType),
            new("IsNviVerified", isNviVerified.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenOptions.SecurityKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var expiration = DateTime.UtcNow.AddMinutes(_tokenOptions.AccessTokenExpirationMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _tokenOptions.Issuer,
            audience: _tokenOptions.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );

        return new AccessToken
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            Expiration = expiration,
            RefreshToken = GenerateRefreshToken(),
            RefreshTokenExpiration = DateTime.UtcNow.AddDays(_tokenOptions.RefreshTokenExpirationDays)
        };
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}