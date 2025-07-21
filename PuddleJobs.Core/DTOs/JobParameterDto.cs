namespace PuddleJobs.Core.DTOs;

public class JobParameterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int JobId { get; set; }
}

public class CreateJobParameterDto
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class JobParameterValueDto
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}

public class JobParameterDefinitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public bool Required { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AssemblyVersionId { get; set; }
} 