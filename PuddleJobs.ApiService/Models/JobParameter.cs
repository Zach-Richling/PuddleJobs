using System.ComponentModel.DataAnnotations;

namespace PuddleJobs.ApiService.Models;

public class JobParameter
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(4000)]
    public string? Value { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign key
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
} 