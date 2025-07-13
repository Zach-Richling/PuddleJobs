using Quartz;

namespace PuddleJobs.ApiService.Models;

public class JobSchedule
{
    public int Id { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign keys
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    
    public int ScheduleId { get; set; }
    public Schedule Schedule { get; set; } = null!;

    /// <summary>
    /// Gets the Quartz trigger key for this job schedule combination
    /// </summary>
    public TriggerKey TriggerKey => new TriggerKey($"trigger_{JobId}_{ScheduleId}", ScheduleId.ToString());
} 