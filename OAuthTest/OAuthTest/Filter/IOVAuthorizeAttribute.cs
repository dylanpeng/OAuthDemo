using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace OAuthTest.Filter
{
    public class IOVAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string scopeClaimType = "urn:oauth:scope";

        private string[] scopes;

        /// <summary>
        /// 权限范围值
        /// </summary>
        public string[] Scopes
        {
            get { return scopes; }
        }

        public IOVAuthorizeAttribute(params string[] scopes)
        {
            if (scopes == null || scopes.Count() == 0)
                this.scopes = new string[] { "common" };
            else
                this.scopes = scopes;
        }

        /// <summary>
        /// 覆盖权限验证方法
        /// </summary>
        /// <param name="actionContext">请求上下文</param>
        /// <returns>是否验证通过</returns>
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {

            return true;
        }
    }
}