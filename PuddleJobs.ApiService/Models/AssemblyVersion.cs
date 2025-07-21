using PuddleJobs.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.ApiService.Models;

public class AssemblyVersion
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string DirectoryPath { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string MainAssemblyName { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }

    public bool IsActive { get; set; } = false;
    
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    // Foreign key
    public int AssemblyId { get; set; }
    public Assembly Assembly { get; set; } = null!;
    
    // Navigation properties
    public ICollection<AssemblyParameterDefinition> ParameterDefinitions { get; set; } = new List<AssemblyParameterDefinition>();

    public static AssemblyVersionDto CreateDto(AssemblyVersion assemblyVersion)
    {
        return new AssemblyVersionDto
        {
            Id = assemblyVersion.Id,
            Version = assemblyVersion.Version,
            FileName = assemblyVersion.MainAssemblyName,
            UploadedAt = assemblyVersion.UploadedAt,
            ChangeNotes = assemblyVersion.ChangeNotes,
            AssemblyId = assemblyVersion.AssemblyId,
            IsActive = assemblyVersion.IsActive
        };
    }
} 