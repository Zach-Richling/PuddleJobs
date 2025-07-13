using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.ApiService.Models;

public class ExecutionLog
{
    public int Id { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Running, Completed, Failed, Cancelled
    
    [MaxLength(4000)]
    public string? Output { get; set; }
    
    [MaxLength(4000)]
    public string? Exception { get; set; }
    
    // Foreign keys
    public int JobId { get; set; }
    public Job? Job { get; set; }
    
    public int? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }
} 