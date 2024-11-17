using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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
    public static string GenerateToken(User user, IConfiguration configuration, TokenType tokenType)
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
                expires = DateTime.UtcNow.AddHours(Convert.ToDouble(configuration["JWT:RefreshTokenExpiry"]));
                break;

            case TokenType.ACCESS:
                expires = DateTime.UtcNow.AddHours(Convert.ToDouble(configuration["JWT:AccessTokenExpiry"]));
                claims.Add(new(ClaimTypes.Email, user.Email));
                claims.Add(new(ClaimTypes.NameIdentifier, user.Id.ToString()));
                break;

            case TokenType.RESET:
                expires = DateTime.UtcNow.AddMinutes(10);
                claims.Add(new("purpose", "reset-password"));
                claims.Add(new(ClaimTypes.Email, user.Email));
                break;
        }

        claims.Add(new(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString(),
        ClaimValueTypes.Integer64));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["JWT:Issuer"],
            audience: configuration["JWT:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static AuthResponseDto GenerateTokens(User user, IConfiguration configuration)
    {
        return new AuthResponseDto
        {
            Access = GenerateToken(user, configuration, TokenType.ACCESS),
            Refresh = GenerateToken(user, configuration, TokenType.REFRESH)
        };
    }

    public static ClaimsPrincipal? ValidateToken(string refreshToken, IConfiguration configuration)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["JWT:Key"]!);

        try
        {
            var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
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
