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
    public string DirectoryPath { get; set; } = string.Empty; // Path to the version directory containing all files
    
    [MaxLength(255)]
    public string MainAssemblyName { get; set; } = string.Empty; // Name of the main assembly file
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }

    public bool IsActive { get; set; } = false; // Indicates if this is the active version for the assembly
    
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    // Foreign key
    public int AssemblyId { get; set; }
    public Assembly Assembly { get; set; } = null!;
    
    // Navigation properties
    public ICollection<AssemblyParameterDefinition> ParameterDefinitions { get; set; } = new List<AssemblyParameterDefinition>();
} 