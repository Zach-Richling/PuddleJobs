using System.Reflection;
using PuddleJobs.Core;
using Quartz;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PuddleJobs.ApiService.Helpers;

public static class JobParameterHelper
{
    public static object? ConvertJobParameterValue(string? value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            return targetType switch
            {
                // Non-nullable types
                var t when t == typeof(string) => value,
                var t when t == typeof(char) => char.Parse(value),
                var t when t == typeof(int) => int.Parse(value),
                var t when t == typeof(long) => long.Parse(value),
                var t when t == typeof(double) => double.Parse(value),
                var t when t == typeof(DateTime) => DateTime.Parse(value),
                var t when t == typeof(TimeOnly) => TimeOnly.Parse(value),
                var t when t == typeof(DateOnly) => DateOnly.Parse(value),
                var t when t == typeof(Guid) => Guid.Parse(value),
                
                // Nullable types
                var t when t == typeof(char?) => char.Parse(value),
                var t when t == typeof(int?) => int.Parse(value),
                var t when t == typeof(long?) => long.Parse(value),
                var t when t == typeof(double?) => double.Parse(value),
                var t when t == typeof(DateTime?) => DateTime.Parse(value),
                var t when t == typeof(TimeOnly?) => TimeOnly.Parse(value),
                var t when t == typeof(DateOnly?) => DateOnly.Parse(value),
                var t when t == typeof(Guid?) => Guid.Parse(value),
                
                _ => throw new InvalidOperationException($"Unsupported type: {targetType.Name}")
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Could not convert '{value}' to type {targetType.Name}. {ex.Message}");
        }
    }

    public static object? ConvertJobParameterValue(string? value, string targetType)
    {
        if (targetType == null)
        {
            throw new InvalidOperationException($"Unknown type: null");
        }

        var type = Type.GetType(targetType) 
            ?? throw new InvalidOperationException($"Unknown type: {targetType}");
        
        return ConvertJobParameterValue(value, type);
    }
}

public class JobParameterInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; } = "";
} 