using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;

namespace AuthServer.MiddleWare
{
    public class BaseUrlMiddleWare
    {
        private readonly RequestDelegate _next;

        public BaseUrlMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var realBaseUrl = context.Request.Headers["X-real-base-url"];
            var url = realBaseUrl.FirstOrDefault();

            if (url != null)
            {
                Uri uriAddress = new Uri(url, UriKind.Absolute);

                context.Request.Scheme = uriAddress.Scheme;
                context.Request.PathBase = new PathString(uriAddress.LocalPath);
                context.Request.Host = new HostString(uriAddress.Host, uriAddress.Port);
                context.SetIdentityServerBasePath(uriAddress.LocalPath);
            }

            await _next(context);
        }
    }
}
