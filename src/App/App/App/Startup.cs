﻿using System;
using System.Collections.Generic;
using Api.Controllers;
using App.Config;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Handlers;
using Infrastructure.Api.Helpers.Implementations;
using Infrastructure.Extensions;
using Infrastructure.Logger;
using Infrastructure.MiddleWare;
using Infrastructure.Session.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Services;
using Swashbuckle.AspNetCore.Swagger;

namespace App
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private IContainer _container;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.Configure<Connections>(Configuration.GetSection("Connections"));
            services.AddSingleton(resolver =>
                resolver.GetRequiredService<IOptions<Connections>>().Value);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });

                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "password",
                    TokenUrl = "http://docker:80/auth/connect/token",
                    Scopes = new Dictionary<string, string>{}
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "oauth2", new string[] { } }
                });
            });

            services.AddMvcCore()
                .AddJsonFormatters()
                .AddJsonOptions(options => options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc)
                .AddApiExplorer() // to Swagger UI
                .AddApplicationPart(typeof(TestController).Assembly)
                .AddCors();

            services.AddHttpClient();

            var builder = new ContainerBuilder();
            builder.Populate(services);

            builder.RegisterType<SessionStorage>().InstancePerLifetimeScope().AsImplementedInterfaces();
            builder.RegisterType<HttpClientHelper>().AsImplementedInterfaces();
            builder.RegisterSource(new CustomLoggerRegistrator());

            builder.RegisterModule<ServiceRegistrationModule>();
            builder.RegisterModule<HandlerRegistrationModule>();

            builder.AddEventBus();

            builder.RegisterType<OnStart>().AsImplementedInterfaces();
            _container = builder.Build();
            return new AutofacServiceProvider(this._container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(
                builder => builder.AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
//                    .AllowCredentials()
            );

            var baseUrl = "/api/app";

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

            app.UseMiddleware<SessionMiddleWare>();
            app.UseMiddleware<LoggingMiddleWare>();

            app.UseMvc();
        }
    }
}
