using Bee.Util;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;

namespace Bee.Auth
{
    public class LoginInfoManager
    {
        public static readonly string Jwt_Authorization = "Authorization";
        public static readonly string Jwt_SecurityKey = "jwtkey";
        public static readonly string Jwt_AccountId = "id";

        private string jwtkey = string.Empty;

        private static readonly LoginInfoManager instance = new LoginInfoManager();
        private LoginInfoManager()
        {
            jwtkey = ConfigUtil.GetAppSettingValue<string>(Jwt_SecurityKey);
        }

        public static LoginInfoManager Instance
        {
            get
            {
                return instance;
            }
        }

        public LoginInfo LoginInfo
        {
            get
            {
                LoginInfo loginInfo = new LoginInfo();
                string jwt = string.Empty;
                var heads = HttpContextUtil.CurrentHttpContext.Request.Headers;
                jwt = heads[LoginInfoManager.Jwt_Authorization];

                if (!string.IsNullOrEmpty(jwt))
                {
                    if(jwt.Length < 7) // jwt 太短了
                    {
                        ThrowExceptionUtil.ThrowHttpCodeException(403, "invalid jwt");
                    }

                    if(jwt.StartsWith("Bearer"))
                    {
                        jwt = jwt.Substring(7);
                    }

                    string accountId = ParseToken(jwt);
                    loginInfo.AccountId = accountId;
                }
                else
                {
                    ThrowExceptionUtil.ThrowHttpCodeException(403, "invalid jwt");
                }
                return loginInfo;
            }
        }

        public string JwtToken(string id)
        {
            var token = new JwtBuilder()
                 .WithAlgorithm(new HMACSHA256Algorithm())
                 .WithSecret(jwtkey)
                 .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
                 .AddClaim(Jwt_AccountId, id)
                 .Build();

            return token;
        }

        private string ParseToken(string token)
        {
            var payload = new JwtBuilder()
                .WithSecret(jwtkey)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(token);

            return payload[Jwt_AccountId].ToString();
        }
        
    }
}
