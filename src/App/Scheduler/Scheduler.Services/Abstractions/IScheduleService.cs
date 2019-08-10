using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Abstractions;
using Infrastructure.Result.Abstraction;
using Scheduler.Commands;

namespace Scheduler.Services.Abstractions
{
    public interface IScheduleService: IService
    {
        Task<IResult<bool>> ScheduleTask(CreateTaskCommand command);
        Task<IResult<bool>> RunTask(object task, string type);
    }
}
