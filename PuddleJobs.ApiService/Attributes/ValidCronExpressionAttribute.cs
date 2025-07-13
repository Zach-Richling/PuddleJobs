using System.ComponentModel.DataAnnotations;
using PuddleJobs.ApiService.Services;

namespace PuddleJobs.ApiService.Attributes;

public class ValidCronExpressionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Cron expression cannot be null");
        }

        var cronExpression = value.ToString();
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return new ValidationResult("Cron expression cannot be empty");
        }

        // Get the validation service from the service provider
        var serviceProvider = validationContext.GetService(typeof(ICronValidationService));
        if (serviceProvider is ICronValidationService validationService)
        {
            var result = validationService.ValidateCronExpression(cronExpression);
            if (!result.IsValid)
            {
                return new ValidationResult(result.ErrorMessage);
            }
        }
        else
        {
            // Fallback validation if service is not available
            try
            {
                Quartz.CronExpression.ValidateExpression(cronExpression);
            }
            catch (Exception ex)
            {
                return new ValidationResult($"Invalid Cron expression: {ex.Message}");
            }
        }

        return ValidationResult.Success;
    }
} 