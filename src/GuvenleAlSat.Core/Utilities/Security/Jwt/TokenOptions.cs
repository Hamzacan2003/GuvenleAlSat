using System;
using System.Collections.Generic;
using System.Text;

namespace GuvenleAlSat.Core.Utilities.Security.Jwt;

public class TokenOptions
{
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public string SecurityKey { get; set; } = string.Empty;
}
