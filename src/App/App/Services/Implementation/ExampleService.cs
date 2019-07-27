﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dtos;
using Events;
using Infrastructure.Abstractions;
using Infrastructure.Api.Helpers;
using Infrastructure.Api.Helpers.Abstractions;
using Infrastructure.Result.Abstraction;
using Infrastructure.Result.Implementation;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Services.Abstractions;

namespace Services.Implementation
{
    internal sealed class ExampleService : IExampleService
    {
        private readonly ILogger<ExampleService> _logger;
        private readonly IEventBus _bus;
        private readonly IHttpClientHelper _httpClientHelper;

        public ExampleService(ILogger<ExampleService> logger, IEventBus bus, IHttpClientHelper httpClientHelper)
        {
            _logger = logger;
            _bus = bus;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<IResult<OutputDto>> DoSomething(InputDto input)
        {
            _logger.LogInformation("log inside DoSomething");
            await _bus.Publish(new TestEvent() {Content = "event content"});

//            return new Fail<OutputDto>(new Exception("test"));
            //           throw new Exception("something went wrong");

            var result = await _httpClientHelper.Get<OutputDto>("http://localhost:5000/api/internal/do-something");
            return new Success<OutputDto>(result);
        }

        public async Task<IResult<OutputDto>> DoSomethingInternal()
        {
            _logger.LogWarning("log inside internal DoSomethingInternal");
            _logger.LogInformation("log inside DoSomethingInternal");
            var result = new OutputDto()
            {
                SomeParam = "outputValue"
            };
            return new Success<OutputDto>(result);
        }
    }
}