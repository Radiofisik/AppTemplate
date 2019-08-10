using Autofac;
using Events;
using Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;
using Scheduler.Commands;
using Services.Abstractions;

namespace App
{
    public class OnStart: IStartable
    {
        private readonly ILogger _logger;
        private readonly IExampleService _exampleService;
        private IEventBus _bus;

        public OnStart(ILogger logger, IExampleService exampleService, IEventBus bus)
        {
            _logger = logger;
            _exampleService = exampleService;
            _bus = bus;
        }

        public void Start()
        {
            _logger.LogInformation("test on start");

//            var result = _exampleService.DoSomething(new InputDto() {SomeParam = "inputValue"});

            _bus.Publish(new CreateTaskCommand("test", "test", "0 */2 * * * ?", new TestEvent()));
        }
    }
}
