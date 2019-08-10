using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Abstractions;
using Infrastructure.Session.Abstraction;
using Rebus.Bus;

namespace Infrastructure.Rebus
{
    public class EventBus: IEventBus
    {
        private readonly IBus _bus;
        private readonly ISessionStorage _sessionStorage;

        public EventBus(IBus bus, ISessionStorage sessionStorage)
        {
            _bus = bus;
            _sessionStorage = sessionStorage;
        }

        public Task Publish<TEvent>(TEvent @event)
        {
            return _bus.Publish(@event, _sessionStorage.GetTraceHeaders());
        }

        public Task Publish(object command, string type)
        {
            var headers = new Dictionary<string, string>
            {
                ["rbs2-content-type"] = "application/json;charset=utf-8",
                ["rbs2-msg-type"] = type,
                ["content_type"] = "application/json;charset=utf-8"
            };

            var traceHeaders = _sessionStorage.GetTraceHeaders();

            foreach (var header in traceHeaders)
            {
                headers[header.Key] = header.Value;
            }

            return _bus.Advanced.Topics.Publish(
                type,
                command,
                headers
            );
        }
    }
}
