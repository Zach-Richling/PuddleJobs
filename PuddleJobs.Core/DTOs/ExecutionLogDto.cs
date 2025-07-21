namespace PuddleJobs.Core.DTOs;

public class ExecutionLogDto
{
    public int JobId { get; set; }
    public long FireInstanceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
}