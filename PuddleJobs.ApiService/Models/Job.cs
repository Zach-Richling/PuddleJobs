using System.ComponentModel.DataAnnotations;
using Quartz;

namespace PuddleJobs.ApiService.Models;

public class Job
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    public int AssemblyId { get; set; }
    public Assembly Assembly { get; set; } = null!;
    
    // Navigation properties
    public ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();
    public ICollection<ExecutionLog> ExecutionLogs { get; set; } = new List<ExecutionLog>();
    public ICollection<JobParameter> Parameters { get; set; } = new List<JobParameter>();

    public JobKey JobKey => new JobKey($"job_{Id}");
    public static JobKey GetJobKey(int jobId) => new JobKey($"job_{jobId}");
} 