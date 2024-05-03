using APIFileServer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Utils.JWTAuthentication;

namespace JWTAuthentication
{
    public class JwtUtils
    {
        JWTSecureConfiguration _config;
        public JwtUtils(JWTSecureConfiguration config) 
        {
            _config = config;
        }

        public JwtSecurityToken CreateToken(UserInfo user)
        {


            var claims = new[] {
                        new Claim(JwtRegisteredClaimNames.Sub, _config.Subject),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                        new Claim("UserId", user.UserId.ToString()),
                        new Claim("DisplayName", user.DisplayName),
                        new Claim("UserName", user.UserName),
                        new Claim("Email", user.Email)
                    };


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.MyKey));
            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _config.Issuer,
                _config.Audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signIn);


            return token;
        }


    }
}
