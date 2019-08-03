using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace Infrastructure.Extensions
{
    public static class Swagger
    {
        public static void AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });

                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "password",
                    TokenUrl = "http://docker:80/auth/connect/token",
                    Scopes = new Dictionary<string, string> { }
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "oauth2", new string[] { } }
                });
            });
        }

        public static void UseCustomSwagger(this IApplicationBuilder app, string baseUrl)
        {
            app.UseSwagger(c=>c.PreSerializeFilters.Add((doc, req) =>
            {
                doc.BasePath = baseUrl;
            }));
         
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{baseUrl}/swagger/v1/swagger.json", "My API V1");
                c.OAuthClientId("client");
                c.OAuthClientSecret("secret");
                c.OAuthRealm("test-realm");
                c.OAuthAppName("test-app");
                c.OAuthScopeSeparator(" ");
                c.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            });
        }
    }
}
