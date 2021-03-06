﻿using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Rebus.Bus;
using Scheduler.Commands;

namespace Scheduler.Handlers
{
    internal class EventSubscriber : IStartable
    {
        private readonly IBus _bus;

        public EventSubscriber(IBus bus)
        {
            _bus = bus;
        }

        public void Start()
        {
            _bus.Subscribe<CreateTaskCommand>().Wait();
        }
    }
}
