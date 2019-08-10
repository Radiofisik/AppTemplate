using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Abstractions;
using Infrastructure.Result.Abstraction;
using Infrastructure.Result.Implementation;
using Infrastructure.Session.Abstraction;
using Newtonsoft.Json;
using Quartz;
using Scheduler.App.Jobs;
using Scheduler.Commands;
using Scheduler.Services.Abstractions;

namespace Scheduler.Services.Implementations
{
    internal sealed class ScheduleService: IScheduleService
    {
        private readonly IEventBus _bus;
        private readonly IScheduler _scheduler;
        private readonly ISessionStorage _sessionStorage;

        public ScheduleService(IEventBus bus, IScheduler scheduler, ISessionStorage sessionStorage)
        {
            _bus = bus;
            _scheduler = scheduler;
            _sessionStorage = sessionStorage;
        }

        public async Task<IResult<bool>> ScheduleTask(CreateTaskCommand command)
        {
            string key = command.Key;
            string group = command.Group;

            IJobDetail jobDetail =
                JobBuilder.Create<ScheduledEventOccuredJob>()
                    .WithIdentity($"job_{key}", group)
                    .UsingJobData("command", command.Command.ToString())
                    .UsingJobData("type", command.Type)
                    .UsingJobData("log-context", JsonConvert.SerializeObject(_sessionStorage.GetTraceHeaders()))
                    .RequestRecovery(true)
                    .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity($"trigger_{key}", group)
                .ForJob(jobDetail)
                .WithCronSchedule(command.CronString)
                .StartNow()
                .Build();

            await _scheduler.ScheduleJob(jobDetail, trigger);
            return new Success<bool>(true);
        }

        public async Task<IResult<bool>> RunTask(object task, string type)
        {
           await _bus.Publish(task, type);
           return new Success<bool>(true);
        }
    }
}
