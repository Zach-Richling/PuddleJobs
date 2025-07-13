namespace PuddleJobs.ApiService.DTOs;

public class ExecutionLogDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Output { get; set; }
    public string? Exception { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public int? ScheduleId { get; set; }
    public string? ScheduleName { get; set; }
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}

public class ExecutionLogSummaryDto
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int RunningExecutions { get; set; }
    public TimeSpan? AverageDuration { get; set; }
} 