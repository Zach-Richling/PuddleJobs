namespace PuddleJobs.Core.DTOs;

public class AssemblyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssemblyDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MainAssemblyName { get; set; } = string.Empty;
}

public class AssemblyVersionDto
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ChangeNotes { get; set; }
    public int AssemblyId { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAssemblyVersionDto
{
    public string Version { get; set; } = string.Empty;
    public string MainAssemblyName { get; set; } = string.Empty;
    public string? ChangeNotes { get; set; }
}

public class SetActiveVersionDto
{
    public int VersionId { get; set; }
} 