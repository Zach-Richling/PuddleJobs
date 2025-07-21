namespace PuddleJobs.Core;

/// <summary>
/// Attribute to define job parameters with type validation.
/// Only the following types are supported: string, char, int, long, double, DateTime, TimeOnly, DateOnly, Guid
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JobParameterAttribute : Attribute
{
    /// <summary>
    /// Gets the list of supported types for job parameters.
    /// </summary>
    public static readonly Type[] SupportedTypes =
    [
        typeof(string),
        typeof(char),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(DateTime),
        typeof(TimeOnly),
        typeof(DateOnly),
        typeof(Guid)
    ];

    /// <summary>
    /// Gets the list of supported nullable types for job parameters.
    /// </summary>
    public static readonly Type[] SupportedNullableTypes =
    [
        typeof(string),
        typeof(char?),
        typeof(int?),
        typeof(long?),
        typeof(double?),
        typeof(DateTime?),
        typeof(TimeOnly?),
        typeof(DateOnly?),
        typeof(Guid?)
    ];

    public string Name { get; }
    public Type Type { get; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public string Description { get; set; } = "";

    /// <summary>
    /// Initializes a new instance of the JobParameterAttribute with the specified name and type.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter. Must be one of the supported types.</param>
    /// <exception cref="ArgumentException">Thrown when the specified type is not supported.</exception>
    public JobParameterAttribute(string name, Type type)
    {
        ValidateType(type);
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Initializes a new instance of the JobParameterAttribute with the specified name, type, and required flag.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter. Must be one of the supported types.</param>
    /// <param name="required">Whether the parameter is required.</param>
    /// <exception cref="ArgumentException">Thrown when the specified type is not supported.</exception>
    public JobParameterAttribute(string name, Type type, bool required) : this(name, type)
    {
        Required = required;
    }

    /// <summary>
    /// Initializes a new instance of the JobParameterAttribute with the specified name, type, required flag, and default value.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter. Must be one of the supported types.</param>
    /// <param name="required">Whether the parameter is required.</param>
    /// <param name="defaultValue">The default value for the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the specified type is not supported.</exception>
    public JobParameterAttribute(string name, Type type, bool required, object defaultValue) : this(name, type, required)
    {
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Checks if the specified type is supported for job parameters.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is supported; otherwise, false.</returns>
    public static bool IsTypeSupported(Type type)
    {
        if (type == null)
            return false;

        return SupportedTypes.Contains(type) || SupportedNullableTypes.Contains(type);
    }

    private static void ValidateType(Type type)
    {
        if (type == null)
            throw new ArgumentException("Type cannot be null.", nameof(type));

        if (!IsTypeSupported(type))
        {
            var supportedTypeNames = SupportedTypes.Select(t => t.Name).OrderBy(n => n);
            var supportedNullableTypeNames = SupportedNullableTypes.Select(t => t.Name).OrderBy(n => n);
            
            var allSupportedTypes = supportedTypeNames.Concat(supportedNullableTypeNames).Distinct();
            var supportedTypesList = string.Join(", ", allSupportedTypes);
            
            throw new ArgumentException(
                $"Type '{type.Name}' is not supported for job parameters. " +
                $"Supported types are: {supportedTypesList}.", 
                nameof(type));
        }
    }
} 