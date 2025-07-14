using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.ApiService.Models;

public class AssemblyParameterDefinition
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Type { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(4000)]
    public string? DefaultValue { get; set; }
    
    public bool Required { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int AssemblyVersionId { get; set; }
    public AssemblyVersion? AssemblyVersion { get; set; }
} 