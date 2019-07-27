﻿using System;
using System.Threading.Tasks;
using Autofac;
using Infrastructure.Session.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Infrastructure.Rebus.Steps
{
    public class LoggerStep: IIncomingStep
    {
        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionScope = context.Load<ITransactionContext>();
            var scope = transactionScope.GetOrNull<ILifetimeScope>("current-autofac-lifetime-scope");
            var logger = scope.Resolve<ILogger<LoggerStep>>();
            var sessionStorage = scope.Resolve<ISessionStorage>();

            MessageContext.Current.Headers.TryGetValue("rbs2-sender-address", out string eventSender);
            MessageContext.Current.Headers.TryGetValue("rbs2-msg-type", out string eventType);
            using (logger.BeginScope(sessionStorage.GetLoggingHeaders()))
            {
                logger.LogInformation("Event type {EventType} from {EventSender} headers: {Headers}", eventType, eventSender, JsonConvert.SerializeObject(MessageContext.Current.Headers));
                logger.LogDebug("Event body: {Body}", JsonConvert.SerializeObject(MessageContext.Current.Message.Body));
                await next();
            }
        }
    }
}
