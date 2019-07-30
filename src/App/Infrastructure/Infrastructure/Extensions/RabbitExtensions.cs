using System;
using System.Collections.Generic;
using System.Text;
using App.Config;
using Autofac;
using Infrastructure.Rebus;
using Infrastructure.Rebus.Steps;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;

namespace Infrastructure.Extensions
{
    public static class RabbitExtensions
    {
        public static ContainerBuilder AddEventBus(this ContainerBuilder builder)
        {
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
            return builder;
        }
    }
}
