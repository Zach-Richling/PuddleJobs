namespace PuddleJobs.Core;

public class CronValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static CronValidationResult Success() => new() { IsValid = true };

    public static CronValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}