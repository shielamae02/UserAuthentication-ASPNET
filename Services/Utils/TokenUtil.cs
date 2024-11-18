using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using UserAuthentication_ASPNET.Models;
using UserAuthentication_ASPNET.Models.Dtos;
using UserAuthentication_ASPNET.Models.Entities;

namespace UserAuthentication_ASPNET.Services.Utils;

public enum TokenType
{
    REFRESH,
    ACCESS,
    RESET
}

public class TokenUtil
{
    public static string GenerateToken(User user, JWTSettings jwt, TokenType tokenType)
    {
        var expires = DateTime.UtcNow;

        var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

        switch (tokenType)
        {
            case TokenType.REFRESH:
                expires = expires.AddDays(jwt.RefreshTokenExpiry);
                break;

            case TokenType.ACCESS:
                expires = expires.AddDays(jwt.AccessTokenExpiry);
                claims.Add(new(ClaimTypes.Email, user.Email));
                claims.Add(new(ClaimTypes.NameIdentifier, user.Id.ToString()));
                break;

            case TokenType.RESET:
                expires = expires.AddMinutes(jwt.RefreshTokenExpiry);
                claims.Add(new("purpose", "reset-password"));
                claims.Add(new(ClaimTypes.Email, user.Email));
                break;
        }

        claims.Add(new(JwtRegisteredClaimNames.Exp,
            new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(),
            ClaimValueTypes.Integer64));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static AuthResponseDto GenerateTokens(User user, JWTSettings jwt)
    {
        return new AuthResponseDto
        {
            Access = GenerateToken(user, jwt, TokenType.ACCESS),
            Refresh = GenerateToken(user, jwt, TokenType.REFRESH)
        };
    }

    public static ClaimsPrincipal? ValidateToken(string token, IConfiguration configuration)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["JWT:Key"]!);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["JWT:Audience"],
                ValidateLifetime = true,
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
