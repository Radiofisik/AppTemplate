using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rebus.Extensions;

namespace Scheduler.Commands
{
    public class CreateTaskCommand
    {
        [JsonConstructor]
        public CreateTaskCommand(string key, string @group, string cronString, string type, JObject command)
        {
            Key = key;
            Group = @group;
            CronString = cronString;
            Type = type;
            Command = command;
        }

       
        public CreateTaskCommand(string key, string @group, string cronString, object task)
        {
            Key = key;
            Group = @group;
            CronString = cronString;
            Type = task.GetType().GetSimpleAssemblyQualifiedName();
            Command = JObject.FromObject(task);
        }

        public string Key { get; }

        public string Group { get; }

        public string CronString { get; }

        public string Type { get; }

        public JObject Command { get; }
    }
}
