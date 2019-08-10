using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Formatting.Compact;

namespace Infrastructure.Extensions
{
    public static class Logging
    {
        public static IWebHostBuilder UseCustomSerilog(this IWebHostBuilder builder)
        {
            return builder.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                .MinimumLevel.Information()
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithAssemblyName()
                .WriteTo.Console()
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(hostingContext.Configuration["Connections:ElasticSearchConnectionString"]))
                {
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                    TemplateName = "serilog2",
                    IndexFormat = "serilog-{0:yyyy.MM.dd}"
                })
            );
        }
    }
}
