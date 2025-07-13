using System.ComponentModel.DataAnnotations;
using PuddleJobs.ApiService.Attributes;

namespace PuddleJobs.ApiService.DTOs;

public class ScheduleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ScheduleDto Create(Models.Schedule schedule)
    {
        return new ScheduleDto
        {
            Id = schedule.Id,
            Name = schedule.Name,
            Description = schedule.Description,
            CronExpression = schedule.CronExpression,
            IsActive = schedule.IsActive,
            CreatedAt = schedule.CreatedAt
        };
    }
}

public class CreateScheduleDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [ValidCronExpression]
    public string CronExpression { get; set; } = string.Empty;
}

public class UpdateScheduleDto
{
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [ValidCronExpression]
    public string? CronExpression { get; set; }
    
    public bool? IsActive { get; set; }
} 