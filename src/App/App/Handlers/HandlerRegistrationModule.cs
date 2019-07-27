﻿using System;
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

            builder.RegisterRebus((configurer, context) => configurer
                .Logging(l => l.Serilog())
                .Transport(t =>
                {
                    var connections = context.Resolve<Connections>();
                    t.UseRabbitMq(connections.RabbitConnectionString, connections.Queue);
                })
                .Options(o => {
                    o.Decorate<IPipeline>(ctx =>
                    {
                        var step = new LoggerStep();
                        var pipeline = ctx.Get<IPipeline>();
                        return new PipelineStepInjector(pipeline).OnReceive(step, PipelineRelativePosition.After, typeof(ActivateHandlersStep));
                    });
                    o.Decorate<IPipeline>(ctx =>
                    {
                        var step = new HeadersIncomingStep();
                        var pipeline = ctx.Get<IPipeline>();
                        return new PipelineStepInjector(pipeline).OnReceive(step, PipelineRelativePosition.Before, typeof(LoggerStep));
                    });
                    o.LogPipeline(true);
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(30);
                }));
            builder.RegisterType<EventBus>().AsImplementedInterfaces();
            builder.RegisterType<EventSubscriber>().AsImplementedInterfaces();

        }
    }
}
