using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;
using GuvenleAlSat.Core.Utilities.Security.Jwt;

namespace GuvenleAlSat.Business.Abstract;

public interface IAuthService
{
    Task<IDataResult<AccessToken>> RegisterAsync(RegisterDto dto, string ipAddress);
    Task<IDataResult<AccessToken>> LoginAsync(LoginDto dto, string ipAddress);
    Task<IDataResult<AccessToken>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<IResult> RevokeTokenAsync(string refreshToken, string ipAddress);
}