using Quartz;
using PuddleJobs.ApiService.Models;
using PuddleJobs.Core;

namespace PuddleJobs.ApiService.Services;

public interface ICronValidationService
{
    /// <summary>
    /// Validates a Cron expression
    /// </summary>
    /// <param name="cronExpression">The Cron expression to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidCronExpression(string cronExpression);
    
    /// <summary>
    /// Validates a Cron expression and returns detailed error information
    /// </summary>
    /// <param name="cronExpression">The Cron expression to validate</param>
    /// <returns>Validation result with error details if invalid</returns>
    CronValidationResult ValidateCronExpression(string cronExpression);
    
    /// <summary>
    /// Gets the next few execution times for a Cron expression
    /// </summary>
    /// <param name="cronExpression">The Cron expression</param>
    /// <param name="count">Number of next executions to return</param>
    /// <returns>List of next execution times</returns>
    IEnumerable<DateTime> GetNextExecutionTimes(string cronExpression, int count = 5);
}

public class CronValidationService : ICronValidationService
{
    public bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        try
        {
            CronExpression.ValidateExpression(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public CronValidationResult ValidateCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return CronValidationResult.Failure("Cron expression cannot be empty or null");
        }

        try
        {
            CronExpression.ValidateExpression(cronExpression);
            return CronValidationResult.Success();
        }
        catch (Exception ex)
        {
            return CronValidationResult.Failure($"Invalid Cron expression: {ex.Message}");
        }
    }

    public IEnumerable<DateTime> GetNextExecutionTimes(string cronExpression, int count = 5)
    {
        if (!IsValidCronExpression(cronExpression))
        {
            return [];
        }

        try
        {
            var cron = new CronExpression(cronExpression);
            var nextTimes = new List<DateTime>();

            var currentTime = DateTime.UtcNow;
            for (int i = 0; i < count; i++)
            {
                var nextTime = cron.GetNextValidTimeAfter(currentTime);
                if (nextTime.HasValue)
                {
                    nextTimes.Add(nextTime.Value.DateTime);
                    currentTime = nextTime.Value.DateTime;
                    continue;
                }
                break;
            }

            return nextTimes;
        }
        catch
        {
            return [];
        }
    }
} 