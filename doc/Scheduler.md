---
title: Планировщик
description: Quarz, Cron...
---
# Cron

В linux задача планирования решается с помощью cron. В файл конфигурации `/etc/crontab` прописываются задачи в формате

```
* * * * * выполняемая команда
- - - - -
| | | | |
| | | | ----- День недели (0 - 7) (Воскресенье =0 или =7)
| | | ------- Месяц (1 - 12)
| | --------- День (1 - 31)
| ----------- Час (0 - 23)
------------- Минута (0 - 59)
```

Примеры заданий из википедии

```bash
# выполнять каждый день в 0 часов 5 минут, результат складывать в log/daily
5 0 * * * $HOME/bin/daily.job >> $HOME/log/daily 2>&1

# выполнять 1 числа каждого месяца в 14 часов 15 минут
15 14 1 * * $HOME/bin/monthly

# каждый рабочий день в 22:00
0 22 * * 1-5 echo "Пора домой"

23 */2 * * * echo "Выполняется в 0:23, 2:23, 4:23 и т. д."
5 4 * * sun echo "Выполняется в 4:05 в воскресенье"
0 0 1 1 * echo "С новым годом!"
15 10,13 * * 1,4 echo "Эта надпись выводится в понедельник и четверг в 10:15 и 13:15"
0-59 * * * * echo "Выполняется ежеминутно"
0-59/2 * * * * echo "Выполняется по четным минутам"
1-59/2 * * * * echo "Выполняется по нечетным минутам"

# каждые 5 минут
*/5 * * * * echo "Прошло пять минут"

# каждое первое воскресенье каждого месяца. -eq 7 это код дня недели, т.е. 1 -> понедельник , 2 -> вторник и т.д.
0 1 1-7 * * [ "$(date '+\%u')" -eq 7 ] && echo "Эта надпись выводится каждое первое воскресенье каждого месяца в 1:00"
```

Данный формат стал стандартом в области планирования задач настолько что его стали реализовывать библиотеки для планирования задач внутри приложения, такие как Quarz.

## Quarz

Поставим задачу создать микросервис которому через шину можно поставить задачу о создании задания. По наступлении времени этого задания микросервис выкидывает в шину то что ему сказали в первоначальном сообщении. Начнем как всегда с установки пакетов

```bash
dotnet add package Autofac.Extras.Quartz
dotnet add package Quartz
dotnet add package Quartz.Serialization.Json
```

Зарегистрируем Quarz и классы заданий

```c#
builder.RegisterModule(new QuartzAutofacFactoryModule());
builder.RegisterModule(new QuartzAutofacJobsModule(typeof(ScheduledEventOccuredJob).Assembly));
```

При старте приложения запустим планировщик

```c#
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
```

Для добавления задачи используем

```c#
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
```

При срабатывание триггера вызывается метод `Execute` в классе

```c#
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
```

и тут есть нюанс, для переотправки события через Rabus создать экземпляр класса команды не возможно, так как нет доступа к сборке с этой командой, поэтому будем использовать особенности работы ребуса и создадим специальный метод для отправки, использующий строковое представление типа

```c#
  public Task Publish(object command, string type)
  {
      var headers = new Dictionary<string, string>
      {
          ["rbs2-content-type"] = "application/json;charset=utf-8",
          ["rbs2-msg-type"] = type,
          ["content_type"] = "application/json;charset=utf-8"
      };

      var traceHeaders = _sessionStorage.GetTraceHeaders();

      foreach (var header in traceHeaders)
      {
          headers[header.Key] = header.Value;
      }

      return _bus.Advanced.Topics.Publish(
          type,
          command,
          headers
      );
  }
```

сама команда выглядит так 

```c#
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
```

метод `GetSimpleAssemblyQualifiedName()` это метод библиотеки `Rebus` который получает ключ маршрутизации, аналогичный тому что получит `Rebus` если скормить ему объект команды.

Пример создания задания, которое срабатывает каждые 2 минуты

```c#
  _bus.Publish(new CreateTaskCommand("test", "test", "0 */2 * * * ?", new TestEvent()));
```



> Проект https://github.com/Radiofisik/AppTemplate tag scheduler

