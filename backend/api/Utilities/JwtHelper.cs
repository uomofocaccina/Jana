using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JanaApi.Utilities;

public class JwtHelper(string _JWTKey, string _JWTIssuer, string _JTWAudience, double _JWTExpiration) : IJwtHelper
{
    private readonly string JWTKey = _JWTKey;
    private readonly string JWTIssuer = _JWTIssuer;
    private readonly string JTWAudience = _JTWAudience;
    private readonly double JWTExpiration = _JWTExpiration;

    public JwtSecurityToken GetJwtToken(Claim[] additionalClaims = null)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (additionalClaims is not null)
        {
            var claimList = new List<Claim>(claims);
            claimList.AddRange(additionalClaims);
            claims = [.. claimList];
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            issuer: JWTIssuer,
        audience: JTWAudience,
            expires: DateTime.UtcNow.Add(TimeSpan.FromDays(JWTExpiration)),
            claims: claims,
            signingCredentials: creds
        );
    }
}
