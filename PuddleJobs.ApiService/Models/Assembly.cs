using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.ApiService.Models;

public class Assembly
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public AssemblyVersion ActiveVersion => Versions.FirstOrDefault(x => x.IsActive) ?? throw new InvalidOperationException($"Assembly {Id} has no active version.");

    // Navigation properties
    public ICollection<AssemblyVersion> Versions { get; set; } = new List<AssemblyVersion>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
} 