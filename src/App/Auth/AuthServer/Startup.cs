using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Config;
using AuthServer.MiddleWare;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Infrastructure.Api.Helpers.Implementations;
using Infrastructure.Extensions;
using Infrastructure.Logger;
using Infrastructure.MiddleWare;
using Infrastructure.Session.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace AuthServer
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

            services.AddIdentityServer()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApis())
                .AddInMemoryClients(Config.GetClients())
                .AddTestUsers(Config.GetUsers())
                .AddDeveloperSigningCredential();


            var builder = new ContainerBuilder();
            builder.Populate(services);

            builder.RegisterType<SessionStorage>().InstancePerLifetimeScope().AsImplementedInterfaces();
            builder.RegisterSource(new CustomLoggerRegistrator());

            builder.AddEventBus();

            _container = builder.Build();
            return new AutofacServiceProvider(this._container);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<BaseUrlMiddleWare>();
            app.UseMiddleware<SessionMiddleWare>();
            app.UseMiddleware<LoggingMiddleWare>();

            app.UseIdentityServer();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
