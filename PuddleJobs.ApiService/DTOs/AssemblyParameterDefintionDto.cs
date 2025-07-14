namespace PuddleJobs.ApiService.DTOs;

/// <summary>
/// Information about a job parameter including its name, type, and metadata.
/// </summary>
public class AssemblyParameterDefintionDto
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the type of the parameter as a string (typically AssemblyQualifiedName).
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the parameter is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the default value for the parameter as a string.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the description of the parameter.
    /// </summary>
    public string? Description { get; set; } = "";
} 