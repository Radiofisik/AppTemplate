﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Config;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.Quartz;
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
using Scheduler.App.Jobs;
using Scheduler.Handlers;
using Scheduler.Services;

namespace Scheduler.App
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

            var builder = new ContainerBuilder();
            builder.Populate(services);

            builder.RegisterType<SessionStorage>().InstancePerLifetimeScope().AsImplementedInterfaces();
            builder.RegisterSource(new CustomLoggerRegistrator());

            builder.RegisterModule<ServiceRegistrationModule>();
            builder.RegisterModule<HandlerRegistrationModule>();

            builder.RegisterModule(new QuartzAutofacFactoryModule());
            builder.RegisterModule(new QuartzAutofacJobsModule(typeof(ScheduledEventOccuredJob).Assembly));

            builder.AddEventBus();

            builder.RegisterType<OnStart>().AsImplementedInterfaces();

            _container = builder.Build();
            return new AutofacServiceProvider(this._container);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
        }
    }
}
