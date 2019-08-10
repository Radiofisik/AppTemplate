using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Rebus.Handlers;
using Scheduler.Commands;
using Scheduler.Services.Abstractions;

namespace Scheduler.Handlers.Handlers
{
    class CreateTaskCommandHandler: IHandleMessages<CreateTaskCommand>
    {
        private IScheduleService _scheduleService;

        public CreateTaskCommandHandler(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        public async Task Handle(CreateTaskCommand message)
        {
            await _scheduleService.ScheduleTask(message);
        }
    }
}
