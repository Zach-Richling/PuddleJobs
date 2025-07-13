namespace PuddleJobs.ApiService.DTOs;

public class JobDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AssemblyId { get; set; }
    public List<ScheduleDto> Schedules { get; set; } = new();

    public static JobDto Create(Models.Job job)
    {
        return new JobDto
        {
            Id = job.Id,
            Name = job.Name,
            Description = job.Description,
            IsActive = job.IsActive,
            CreatedAt = job.CreatedAt,
            AssemblyId = job.AssemblyId,
            Schedules = job.JobSchedules?.Select(js => ScheduleDto.Create(js.Schedule)).ToList() ?? new List<ScheduleDto>()
        };
    }
}

public class CreateJobDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AssemblyId { get; set; }
    public List<int> ScheduleIds { get; set; } = new();
    public List<JobParameterValueDto> Parameters { get; set; } = new();
}

public class UpdateJobDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public List<int> ScheduleIds { get; set; } = [];
    public List<JobParameterValueDto> Parameters { get; set; } = [];
} 