using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Quartz;

namespace Scheduler.App
{
    public class OnStart: IStartable
    {
        private readonly IScheduler _scheduler;

        public OnStart(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Start()
        {
            _scheduler.Start();
        }
    }
}
