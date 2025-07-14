using Serilog.Core;
using Serilog.Events;

namespace PuddleJobs.ApiService.Enrichers;

public class NameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextProperty) &&
            sourceContextProperty is ScalarValue scalarValue &&
            scalarValue.Value is string sourceContext)
        {
            var nameSpace = sourceContext.Split('.').ToList();
            
            var assemblyName = nameSpace[0];
            var assemblyNameProperty = propertyFactory.CreateProperty("AssemblyName", assemblyName);
            logEvent.AddPropertyIfAbsent(assemblyNameProperty);

            var className = nameSpace[^1];
            var classNameProperty = propertyFactory.CreateProperty("ClassName", className);
            logEvent.AddPropertyIfAbsent(classNameProperty);
        }
    }
} 