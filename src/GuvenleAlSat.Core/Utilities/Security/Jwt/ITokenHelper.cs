using System;
using System.Collections.Generic;
using System.Text;

namespace GuvenleAlSat.Core.Utilities.Security.Jwt;

public interface ITokenHelper
{
    AccessToken CreateToken(Guid userId, string email, string fullName, string UserType, bool isNviVerified, IList<string> roles);
    string GenerateRefreshToken();
}
