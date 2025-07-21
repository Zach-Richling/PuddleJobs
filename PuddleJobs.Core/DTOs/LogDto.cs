namespace PuddleJobs.Core.DTOs;
public class LogDto
{
    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Level { get; set; }
    public DateTime? TimeStamp { get; set; }
    public string? Exception { get; set; }
    public string? LogEvent { get; set; }
    public string? ClassName { get; set; }
    public long? FireInstanceId { get; set; }
} 