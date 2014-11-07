using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Owin;
using OAuthTest.Providers;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Infrastructure;
using System.Collections.Concurrent;
using System.Security.Claims;
using OAuthTest.Models;
using System.Web;
using OAuthTest.Common;
using Microsoft.Owin.Security;
using System.Security.Principal;

namespace OAuthTest
{
    public partial class Startup
    {
        private ClientConfig _clientConfig;

        private ClientConfig ClientConfig
        {
            get
            {
                if (_clientConfig == null)
                    _clientConfig = (ClientConfig)XmlFileHelper.LoadXml(HttpRuntime.AppDomainAppPath +
                        "App_Data\\ClientConfig.xml", typeof(ClientConfig));
                return _clientConfig;
            }
        }

        private int accessTokenExpireTimeSpan = int.Parse(System.Configuration.ConfigurationManager.AppSettings["accessTokenExpireTimeSpan"]);
        private bool allowInsecureHttp = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["allowInsecureHttp"]);


        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseOAuthBearerAuthentication(new Microsoft.Owin.Security.OAuth.OAuthBearerAuthenticationOptions());

            #region
            // Enable Application Sign In Cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Application",
                AuthenticationMode = AuthenticationMode.Passive,
                LoginPath = new PathString(Paths.LoginPath),
                LogoutPath = new PathString(Paths.LogoutPath),
            });

            // Enable External Sign In Cookie
            app.SetDefaultSignInAsAuthenticationType("External");
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "External",
                AuthenticationMode = AuthenticationMode.Passive,
                CookieName = CookieAuthenticationDefaults.CookiePrefix + "External",
                ExpireTimeSpan = TimeSpan.FromMinutes(5),
            });

            // Enable google authentication
            app.UseGoogleAuthentication();
            #endregion

            // Setup Authorization Server
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                AuthorizeEndpointPath = new PathString(Paths.AuthorizePath),
                TokenEndpointPath = new PathString(Paths.TokenPath),
                AccessTokenExpireTimeSpan = new TimeSpan(0, 0, accessTokenExpireTimeSpan), //AccessToken 过期时间，目前暂定24小时（86400秒）
                ApplicationCanDisplayErrors = true,
                //#if DEBUG
                AllowInsecureHttp = allowInsecureHttp,
                //#endif
                // Authorization server provider which controls the lifecycle of Authorization Server
                Provider = new OAuthAuthorizationServerProvider
                {
                    OnValidateClientRedirectUri = ValidateClientRedirectUri,
                    OnValidateClientAuthentication = ValidateClientAuthentication,
                    OnGrantResourceOwnerCredentials = GrantResourceOwnerCredentials,
                    OnGrantClientCredentials = GrantClientCredetails,
                    OnGrantRefreshToken = GrantRefreshToken,
                    OnTokenEndpoint = OnTokenEndpoint
                },

                // Authorization code provider which creates and receives authorization code
                AuthorizationCodeProvider = new AuthenticationTokenProvider
                {
                    OnCreate = CreateAuthenticationCode,
                    OnReceive = ReceiveAuthenticationCode,
                },

                // Refresh token provider which creates and receives referesh token
                RefreshTokenProvider = new AuthenticationTokenProvider
                {
                    OnCreate = CreateRefreshToken,
                    OnReceive = ReceiveRefreshToken,
                }
            });
        }

        private Task OnTokenEndpoint(OAuthTokenEndpointContext context)
        {
            context.AdditionalResponseParameters.Add("AccountId", context.Identity.Name);
            return Task.FromResult(0);
        }

        private Task ValidateClientRedirectUri(OAuthValidateClientRedirectUriContext context)
        {
            foreach (Client c in ClientConfig.Clients)
            {
                if (context.ClientId == c.Id)
                {
                    context.Validated(c.RedirectUrl);
                    break;
                }
            }
            return Task.FromResult(0);
        }

        private Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            string clientId;
            string clientSecret;
            bool isValidated = false;
            if (context.TryGetBasicCredentials(out clientId, out clientSecret) ||
                context.TryGetFormCredentials(out clientId, out clientSecret))
            {
                foreach (Client c in ClientConfig.Clients)
                {
                    if (clientId == c.Id && clientSecret == c.Secret)
                    {
                        context.Validated();
                        isValidated = true;
                        break;
                    }
                }
            }
            if (!isValidated)
                context.SetError("客户端验证错误");
            return Task.FromResult(0);
        }

        private Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            //登录名:"ceshi";手机号码:"m:13812345678";电子邮箱:"e:ceshi@vcyber.cn"
            string userName = context.UserName;
            string passWord = context.Password;

            int accountId = 11;
            var identity = new ClaimsIdentity(new GenericIdentity(accountId.ToString(), OAuthDefaults.AuthenticationType),
                context.Scope.Select(x => new Claim("urn:oauth:scope", x)));
            context.Validated(identity);

            return Task.FromResult(0);
        }

        private Task GrantClientCredetails(OAuthGrantClientCredentialsContext context)
        {
            var client = this.ClientConfig.Clients.FirstOrDefault(i => i.Id == context.ClientId);
            if (client == null)
            {
                context.SetError("客户端不存在。");
                return Task.FromResult(0);
            }
            if (!ScopeValidation(context.Scope, client.ClientScope))
            {
                context.SetError("客户端请求的Scope超出定义。");
                return Task.FromResult(0);
            }
            else
            {
                var identity = new ClaimsIdentity(new GenericIdentity(context.ClientId, "Client"),//原值：OAuthDefaults.AuthenticationType 
                    context.Scope.Select(x => new Claim("urn:oauth:scope", x)));
                context.Validated(identity);
                return Task.FromResult(0);
            }
        }


        private readonly ConcurrentDictionary<string, string> _authenticationCodes =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        private void CreateAuthenticationCode(AuthenticationTokenCreateContext context)
        {
            context.SetToken(Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n"));
            _authenticationCodes[context.Token] = context.SerializeTicket();
        }

        private void ReceiveAuthenticationCode(AuthenticationTokenReceiveContext context)
        {
            string value;
            if (_authenticationCodes.TryRemove(context.Token, out value))
            {
                context.DeserializeTicket(value);
            }
        }

        private void CreateRefreshToken(AuthenticationTokenCreateContext context)
        {
            context.SetToken(context.SerializeTicket());
        }

        private void ReceiveRefreshToken(AuthenticationTokenReceiveContext context)
        {
            context.DeserializeTicket(context.Token);
            //修改过期时间和发布时间
            context.Ticket.Properties.ExpiresUtc = new DateTimeOffset(DateTime.UtcNow.AddSeconds(accessTokenExpireTimeSpan));
            context.Ticket.Properties.IssuedUtc = new DateTimeOffset(DateTime.UtcNow);
        }


        private bool ScopeValidation(IEnumerable<string> requestScopes, IEnumerable<string> scopes)
        {
            bool isContains = true;
            foreach (var s in requestScopes)
            {
                if (!scopes.Contains(s))
                {
                    isContains = false;
                    break;
                }
            }
            return isContains;
        }

        /// <summary>
        /// 更新令牌
        /// </summary>
        private Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            context.Validated();
            return Task.FromResult(0);
        }
    }

    public static class Paths
    {
        #region
        /// <summary>
        /// AuthorizationServer project should run on this URL
        /// </summary>
        //public const string AuthorizationServerBaseAddress = "http://localhost:6023";

        /// <summary>
        /// ResourceServer project should run on this URL
        /// </summary>
        //public const string ResourceServerBaseAddress = "http://localhost:38385";

        /// <summary>
        /// ImplicitGrant project should be running on this specific port '38515'
        /// </summary>
        //public const string ImplicitGrantCallBackPath = "http://localhost:38515/Home/SignIn";

        /// <summary>
        /// AuthorizationCodeGrant project should be running on this URL.
        /// </summary>
        //public const string AuthorizeCodeCallBackPath = "http://localhost:38500/";
        #endregion

        public const string AuthorizePath = "/OAuth/Authorize";
        public const string TokenPath = "/OAuth/Token";
        public const string LoginPath = "/Account/Login";
        public const string LogoutPath = "/Account/Logout";
    }
}
