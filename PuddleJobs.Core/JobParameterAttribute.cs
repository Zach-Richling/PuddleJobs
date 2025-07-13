namespace PuddleJobs.Core;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class JobParameterAttribute : Attribute
{
    public string Name { get; }
    public Type Type { get; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public string Description { get; set; } = "";

    public JobParameterAttribute(string name, Type type)
    {
        Name = name;
        Type = type;
    }

    public JobParameterAttribute(string name, Type type, bool required) : this(name, type)
    {
        Required = required;
    }

    public JobParameterAttribute(string name, Type type, bool required, object defaultValue) : this(name, type, required)
    {
        DefaultValue = defaultValue;
    }
} 