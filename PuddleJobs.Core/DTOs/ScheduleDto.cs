using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.Core.DTOs;

public class ScheduleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateScheduleDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public string CronExpression { get; set; } = string.Empty;
}

public class UpdateScheduleDto
{
    [MaxLength(1000)]
    public string? Description { get; set; }
    public string? CronExpression { get; set; }
    
    public bool? IsActive { get; set; }
} 