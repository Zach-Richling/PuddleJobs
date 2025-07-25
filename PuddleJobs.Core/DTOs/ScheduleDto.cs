using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.Core.DTOs;

public class ScheduleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
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

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateScheduleDto
{
    [Required]
    public string Name { get; set; } = "";
    [MaxLength(1000)]
    public string? Description { get; set; }
    public string? CronExpression { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }
} 