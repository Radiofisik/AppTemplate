using Autofac;
using Microsoft.Extensions.Logging;
using Services.Abstractions;

namespace App
{
    public class OnStart: IStartable
    {
        private readonly ILogger _logger;
        private readonly IExampleService _exampleService;

        public OnStart(ILogger logger, IExampleService exampleService)
        {
            _logger = logger;
            _exampleService = exampleService;
        }

        public void Start()
        {
            _logger.LogInformation("test on start");
//            var result = _exampleService.DoSomething(new InputDto() {SomeParam = "inputValue"});
        }
    }
}
