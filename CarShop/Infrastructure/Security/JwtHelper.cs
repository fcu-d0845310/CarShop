using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarShop.Infrastructure.Security
{
    public record TokenResult(string AccessToken, DateTime ExpiresAt);
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration= configuration;
        }

        public TokenResult GenerateToken(int userId,int roleId,bool isProfileCompleted)
        {
            var issuer = _configuration.GetValue<string>("Jwt:Issuer")
                ?? throw new InvalidOperationException("缺少設定 Jwt:Issuer");
            var audience = _configuration.GetValue<string>("Jwt:Audience")
                ?? throw new InvalidOperationException("缺少設定 Jwt:Audience");
            var signKey = _configuration.GetValue<string>("Jwt:Key")
                ?? throw new InvalidOperationException("缺少設定 Jwt:Key");

            // 到期時間
            var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiresMinutes"));

            var claims = new List<Claim>(); 
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));
            claims.Add(new Claim("role", roleId.ToString()));
            claims.Add(new Claim("profile_completed", isProfileCompleted.ToString()));

            var userClaimsIdentity = new ClaimsIdentity(claims);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience,
                Subject = userClaimsIdentity,
                Expires = expiresAt,
                SigningCredentials = signingCredentials
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var serializeToken = tokenHandler.WriteToken(securityToken);

            return new TokenResult(serializeToken, expiresAt);
        }
    }
}
