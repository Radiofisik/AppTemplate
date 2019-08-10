using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Rebus.Handlers;

namespace Scheduler.Handlers
{
    public class HandlerRegistrationModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var types =
                GetType().Assembly.GetTypes()
                    .Where(type => typeof(IHandleMessages).IsAssignableFrom(type))
                    .ToArray();

            builder.RegisterTypes(types)
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterType<EventSubscriber>().AsImplementedInterfaces();

        }
    }
}
