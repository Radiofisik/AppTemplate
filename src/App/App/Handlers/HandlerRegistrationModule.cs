using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Config;
using Autofac;
using Infrastructure.Rebus;
using Infrastructure.Rebus.Steps;
using Infrastructure.Session.Abstraction;
using RabbitMQ.Client.Framing.Impl;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Handlers
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
