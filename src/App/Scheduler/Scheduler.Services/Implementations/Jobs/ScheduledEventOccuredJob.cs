using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Session.Abstraction;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Scheduler.Services.Abstractions;

namespace Scheduler.App.Jobs
{
    public class ScheduledEventOccuredJob: IJob
    {
        private readonly IScheduleService _scheduleService;
        private readonly ISessionStorage _sessionStorage;
        private readonly ILogger _logger;

        public ScheduledEventOccuredJob(IScheduleService scheduleService, ISessionStorage sessionStorage, ILogger logger)
        {
            _scheduleService = scheduleService;
            _sessionStorage = sessionStorage;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var command = JObject.Parse(context.MergedJobDataMap["command"].ToString());
            var type = context.MergedJobDataMap["type"].ToString();
            var logContext = context.MergedJobDataMap["log-context"]?.ToString();

            if (!string.IsNullOrWhiteSpace(logContext))
            {
                var logContextDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(logContext);
                _sessionStorage.SetHeaders(logContextDict);
            }

            using (_logger.BeginScope(_sessionStorage.GetLoggingHeaders()))
            {
                _logger.LogInformation("Running command: {Command} for {Key}_{Group}", context.MergedJobDataMap["command"], context.JobDetail.Key.Name, context.JobDetail.Key.Group);
                await _scheduleService.RunTask(command, type);
            }
        }
    }
}
