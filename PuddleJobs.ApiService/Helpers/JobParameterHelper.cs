using System.Reflection;
using PuddleJobs.Core;
using Quartz;

namespace PuddleJobs.ApiService.Helpers;

public static class JobParameterHelper
{
    public static Dictionary<string, object> GetValidatedParameters(IJobExecutionContext context)
    {
        var jobType = context.JobDetail.JobType;
        var jobData = context.JobDetail.JobDataMap;
        var result = new Dictionary<string, object>();

        // Get parameter attributes from the job type
        var parameterAttributes = jobType.GetCustomAttributes<JobParameterAttribute>(true);

        foreach (var attr in parameterAttributes)
        {
            var value = GetParameterValue(jobData, attr);
            ValidateParameter(value, attr);
            result[attr.Name] = value;
        }

        return result;
    }

    public static T GetParameter<T>(IJobExecutionContext context, string parameterName, T defaultValue = default!)
    {
        var jobData = context.JobDetail.JobDataMap;
        
        if (jobData.ContainsKey(parameterName))
        {
            var value = jobData[parameterName];
            if (value != null)
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        
        return defaultValue;
    }

    public static T GetRequiredParameter<T>(IJobExecutionContext context, string parameterName)
    {
        var jobData = context.JobDetail.JobDataMap;
        
        if (!jobData.ContainsKey(parameterName))
        {
            throw new InvalidOperationException($"Required parameter '{parameterName}' is missing.");
        }
        
        var value = jobData[parameterName];
        if (value == null)
        {
            throw new InvalidOperationException($"Required parameter '{parameterName}' is null.");
        }
        
        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static object GetParameterValue(JobDataMap jobData, JobParameterAttribute attr)
    {
        if (jobData.ContainsKey(attr.Name))
        {
            var value = jobData[attr.Name];
            if (value != null)
            {
                return Convert.ChangeType(value, attr.Type);
            }
        }

        // Return default value if specified
        if (attr.DefaultValue != null)
        {
            return Convert.ChangeType(attr.DefaultValue, attr.Type);
        }

        // Return default for the type
        return attr.Type.IsValueType ? Activator.CreateInstance(attr.Type)! : null!;
    }

    private static void ValidateParameter(object value, JobParameterAttribute attr)
    {
        // Check if required parameter is missing
        if (attr.Required && value == null)
        {
            throw new InvalidOperationException($"Required parameter '{attr.Name}' is missing or null.");
        }

        if (value == null) return; // Skip validation for null values

        // Type validation
        if (!attr.Type.IsInstanceOfType(value))
        {
            throw new InvalidOperationException($"Parameter '{attr.Name}' has invalid type. Expected {attr.Type.Name}, got {value.GetType().Name}.");
        }
    }

    public static JobParameterInfo[] GetJobParameterInfo(Type jobType)
    {
        var attributes = jobType.GetCustomAttributes<JobParameterAttribute>(true);
        
        return attributes.Select(attr => new JobParameterInfo
        {
            Name = attr.Name,
            Type = attr.Type.FullName ?? attr.Type.Name,
            Required = attr.Required,
            DefaultValue = attr.DefaultValue?.ToString(),
            Description = attr.Description
        }).ToArray();
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