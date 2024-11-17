using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using UserAuthentication_ASPNET.Models.Entities;
using UserAuthentication_ASPNET.Models.Dtos;

namespace UserAuthentication_ASPNET.Services.Utils;

public enum TokenType
{
    REFRESH,
    ACCESS,
    RESET
}

public class TokenUtil
{
    private static string GenerateToken(User user, IConfiguration configuration, TokenType tokenType)
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

    public static string GenerateAccess(User user, IConfiguration configuration)
    {
        var expiry = DateTime.UtcNow.AddHours(Convert.ToDouble(configuration["JWT:AccessTokenExpiry"]));

        return GenerateToken(user, configuration, expiry, isAccessToken: true);
    }

    public static string GenerateRefresh(User user, IConfiguration configuration)
    {
        var expiry = DateTime.UtcNow.AddHours(Convert.ToDouble(configuration["JWT:RefreshTokenExpiry"]));

        return GenerateToken(user, configuration, expiry, isAccessToken: false);
    }

    public static string GeneratePasswordResetToken(User user, IConfiguration configuration)
    {
        var expiry = DateTime.UtcNow.AddMinutes(15);

        return GenerateToken(user, configuration, expiry, isAccessToken: false, isPasswordResetToken: true);
    }

    public static AuthResponseDto GenerateTokens(User user, IConfiguration configuration)
    {
        return new AuthResponseDto
        {
            Access = GenerateAccess(user, configuration),
            Refresh = GenerateRefresh(user, configuration)
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
