using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ConsuCare.Shared.Models;
using Microsoft.IdentityModel.Tokens;

namespace ConsuCare.Api.Services;

public class JwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;

    public JwtTokenService(IConfiguration config)
    {
        _key = config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is not configured.");
        _issuer = config["Jwt:Issuer"] ?? "ConsuCare";
    }

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
