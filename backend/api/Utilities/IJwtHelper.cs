using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace JanaApi.Utilities;

public interface IJwtHelper
{
    JwtSecurityToken GetJwtToken(Claim[] additionalClaims = null);
}
