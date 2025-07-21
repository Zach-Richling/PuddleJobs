using PuddleJobs.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.ApiService.Models;

public class Schedule
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string CronExpression { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public ICollection<JobSchedule> JobSchedules { get; set; } = new List<JobSchedule>();

    public static ScheduleDto CreateDto(Schedule schedule)
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