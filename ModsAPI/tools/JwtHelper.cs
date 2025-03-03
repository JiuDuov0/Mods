using Entity.Role;
using Entity.User;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Redis.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ModsAPI.tools
{
    public class JwtHelper
    {
        private readonly IOptionsSnapshot<JwtSettings> _jwtSettings;
        private readonly IRedisManageService _RedisManage;

        public JwtHelper(IOptionsSnapshot<JwtSettings> jwtSettings, IRedisManageService redisManage)
        {
            _jwtSettings = jwtSettings;
            _RedisManage = redisManage;
        }

        /// <summary>
        /// 颁发Token
        /// </summary>
        /// <returns></returns>
        public ResponseToken CreateToken(UserEntity userInfo)
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", userInfo.UserId),
                new Claim("UserRoleIDs", string.Join(",",userInfo.UserRoleID)),
                new Claim("NickName",userInfo.NickName),
            };
            var roleslist = new RoleEntity().GetRoleList();
            foreach (var item in userInfo.UserRoleID)
            {
                var rolename = roleslist.Find(x => x.Id == item);
                claims.Add(new Claim(ClaimTypes.Role, rolename.RoleName));
            }

            string key = _jwtSettings.Value.SecrentKey;
            byte[] secBytes = Encoding.UTF8.GetBytes(key);
            var secKey = new SymmetricSecurityKey(secBytes);

            var tokenDescriptor = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddSeconds(_jwtSettings.Value.Expirces),
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                signingCredentials: new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256Signature)
                );


            string token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            string refreshToken = CreateRefreshToken();

            _RedisManage.SetAsync(userInfo.UserId + "Token", token, new TimeSpan(0, 0, _jwtSettings.Value.Expirces));
            _RedisManage.SetAsync(userInfo.UserId + "RefreshToken", refreshToken, new TimeSpan(0, 0, _jwtSettings.Value.RefreshTokenExpirces));
            return new ResponseToken() { Token = token, Refresh_Token = refreshToken };
        }

        /// <summary>
        /// 生成存活时间最大的token
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public ResponseToken CreateYearsToken(UserEntity userInfo)
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", userInfo.UserId),
                new Claim("UserRoleIDs", string.Join(",",userInfo.UserRoleID)),
                new Claim("NickName",userInfo.NickName),
            };

            string key = _jwtSettings.Value.SecrentKey;
            byte[] secBytes = Encoding.UTF8.GetBytes(key);
            var secKey = new SymmetricSecurityKey(secBytes);

            var tokenDescriptor = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddYears(80),
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                signingCredentials: new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256Signature)
                );


            return new ResponseToken() { Token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor) };
        }

        /// <summary>
        /// 生成refresh_Token
        /// </summary>
        /// <returns></returns>
        public string CreateRefreshToken()
        {
            var refresh = new byte[32];
            using (var number = RandomNumberGenerator.Create())
            {
                number.GetBytes(refresh);
                return Convert.ToBase64String(refresh);
            }
        }

        public ResponseToken Refresh(string Token, string refresh_Token, HttpContext httpContext)
        {
            string key = _jwtSettings.Value.SecrentKey;

            byte[] secBytes = Encoding.UTF8.GetBytes(key);
            var secKey = new SymmetricSecurityKey(secBytes);

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;

            var principal = tokenHandler.ValidateToken(Token, new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secKey,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Value.Audience,
                ValidateLifetime = false
            }, out validatedToken);

            var jwtToken = validatedToken as JwtSecurityToken;

            if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token不合法");
            }

            //string tokenStr = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

            var handler = new JwtSecurityTokenHandler();
            var payload = handler.ReadJwtToken(Token).Payload;
            var claims = payload.Claims;
            var UserId = claims.First(claim => claim.Type == "userid").Value;
            var OldRefreshToken = _RedisManage.GetValue(UserId + "RefreshToken").ToString().Replace("\"", "");
            if (OldRefreshToken == null || refresh_Token != OldRefreshToken)
            {
                throw new SecurityTokenException("Token不合法");
            }

            var jwtSecurityToken = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.Now,
                expires: DateTime.Now.AddSeconds(_jwtSettings.Value.Expirces),
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                signingCredentials: new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256Signature)
                );

            var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            var refreshToken = CreateRefreshToken();

            _RedisManage.SetAsync(UserId + "Token", token, new TimeSpan(0, 0, _jwtSettings.Value.Expirces));
            _RedisManage.SetAsync(UserId + "RefreshToken", refreshToken, new TimeSpan(0, 0, _jwtSettings.Value.RefreshTokenExpirces));

            return new ResponseToken() { Token = token, Refresh_Token = refreshToken };
        }
        /// <summary>
        /// 解析token返回字符串
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public string? GetTokenStr(string Token, string type)
        {
            string key = _jwtSettings.Value.SecrentKey;

            byte[] secBytes = Encoding.UTF8.GetBytes(key);
            var secKey = new SymmetricSecurityKey(secBytes);

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;

            var principal = tokenHandler.ValidateToken(Token, new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secKey,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Value.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Value.Audience,
                ValidateLifetime = false
            }, out validatedToken);

            var jwtToken = validatedToken as JwtSecurityToken;
            return jwtToken?.Claims.FirstOrDefault(x => x.Type == type)?.Value;
        }
    }
    /// <summary>
    /// JWT 配置信息
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// 颁发者
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// 使用者
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// 私钥
        /// </summary>
        public string SecrentKey { get; set; }

        /// <summary>
        /// Token过期时间 单位秒
        /// </summary>
        public int Expirces { get; set; }

        /// <summary>
        /// Refresh_Token过期时间 单位秒
        /// </summary>
        public int RefreshTokenExpirces { get; set; }
    }
    /// <summary>
    /// Token的返回实体
    /// </summary>
    public class ResponseToken
    {

        /// <summary>
        /// JWT Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 用于刷新token的刷新令牌
        /// </summary>
        public string Refresh_Token { get; set; }
    }
}

