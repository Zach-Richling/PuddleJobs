using System;
using System.Linq;
using PuddleJobs.Core;
using Xunit;

namespace PuddleJobs.Tests;

public class JobParameterAttributeTests
{
    [Fact]
    public void JobParameterAttribute_SupportedTypes_ShouldBeCorrect()
    {
        // Arrange & Act
        var supportedTypes = JobParameterAttribute.SupportedTypes;
        var expectedTypes = new[]
        {
            typeof(string),
            typeof(char),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(DateTime),
            typeof(TimeOnly),
            typeof(DateOnly),
            typeof(Guid)
        };

        // Assert
        Assert.Equal(expectedTypes.Length, supportedTypes.Length);
        foreach (var expectedType in expectedTypes)
        {
            Assert.Contains(expectedType, supportedTypes);
        }
    }

    [Fact]
    public void JobParameterAttribute_SupportedNullableTypes_ShouldBeCorrect()
    {
        // Arrange & Act
        var supportedNullableTypes = JobParameterAttribute.SupportedNullableTypes;
        var expectedNullableTypes = new[]
        {
            typeof(string), // string is already nullable
            typeof(char?),
            typeof(int?),
            typeof(long?),
            typeof(double?),
            typeof(DateTime?),
            typeof(TimeOnly?),
            typeof(DateOnly?),
            typeof(Guid?)
        };

        // Assert
        Assert.Equal(expectedNullableTypes.Length, supportedNullableTypes.Length);
        foreach (var expectedType in expectedNullableTypes)
        {
            Assert.Contains(expectedType, supportedNullableTypes);
        }
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(char))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(Guid))]
    public void JobParameterAttribute_SupportedTypes_ShouldCreateSuccessfully(Type supportedType)
    {
        // Act & Assert
        var attribute = new JobParameterAttribute("TestParam", supportedType);
        Assert.Equal("TestParam", attribute.Name);
        Assert.Equal(supportedType, attribute.Type);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(char?))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(TimeOnly?))]
    [InlineData(typeof(DateOnly?))]
    [InlineData(typeof(Guid?))]
    public void JobParameterAttribute_SupportedNullableTypes_ShouldCreateSuccessfully(Type supportedNullableType)
    {
        // Act & Assert
        var attribute = new JobParameterAttribute("TestParam", supportedNullableType);
        Assert.Equal("TestParam", attribute.Name);
        Assert.Equal(supportedNullableType, attribute.Type);
    }

    [Theory]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(float))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(object))]
    [InlineData(typeof(Exception))]
    [InlineData(typeof(JobParameterAttribute))]
    public void JobParameterAttribute_UnsupportedTypes_ShouldThrowArgumentException(Type unsupportedType)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new JobParameterAttribute("TestParam", unsupportedType));
        
        Assert.Contains("is not supported for job parameters", exception.Message);
        Assert.Contains(unsupportedType.Name, exception.Message);
        Assert.Contains("Supported types are:", exception.Message);
    }

    [Fact]
    public void JobParameterAttribute_NullType_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new JobParameterAttribute("TestParam", null!));
        
        Assert.Contains("Type cannot be null", exception.Message);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(char))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(char?))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(TimeOnly?))]
    [InlineData(typeof(DateOnly?))]
    [InlineData(typeof(Guid?))]
    public void IsTypeSupported_SupportedTypes_ShouldReturnTrue(Type supportedType)
    {
        // Act
        var result = JobParameterAttribute.IsTypeSupported(supportedType);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(float))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(object))]
    [InlineData(typeof(Exception))]
    [InlineData(typeof(JobParameterAttribute))]
    public void IsTypeSupported_UnsupportedTypes_ShouldReturnFalse(Type unsupportedType)
    {
        // Act
        var result = JobParameterAttribute.IsTypeSupported(unsupportedType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTypeSupported_NullType_ShouldReturnFalse()
    {
        // Act
        var result = JobParameterAttribute.IsTypeSupported(null);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(char))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(Guid))]
    public void JobParameterAttribute_ConstructorWithRequired_ShouldSetProperties(Type supportedType)
    {
        // Act
        var attribute = new JobParameterAttribute("TestParam", supportedType, true);

        // Assert
        Assert.Equal("TestParam", attribute.Name);
        Assert.Equal(supportedType, attribute.Type);
        Assert.True(attribute.Required);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(char))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(Guid))]
    public void JobParameterAttribute_ConstructorWithDefaultValue_ShouldSetProperties(Type supportedType)
    {
        // Arrange
        var defaultValue = GetDefaultValueForType(supportedType);

        // Act
        var attribute = new JobParameterAttribute("TestParam", supportedType, false, defaultValue);

        // Assert
        Assert.Equal("TestParam", attribute.Name);
        Assert.Equal(supportedType, attribute.Type);
        Assert.False(attribute.Required);
        Assert.Equal(defaultValue, attribute.DefaultValue);
    }

    [Fact]
    public void JobParameterAttribute_Description_ShouldBeSettable()
    {
        // Arrange
        var attribute = new JobParameterAttribute("TestParam", typeof(string));

        // Act
        attribute.Description = "Test description";

        // Assert
        Assert.Equal("Test description", attribute.Description);
    }

    [Fact]
    public void JobParameterAttribute_Required_ShouldBeSettable()
    {
        // Arrange
        var attribute = new JobParameterAttribute("TestParam", typeof(string));

        // Act
        attribute.Required = true;

        // Assert
        Assert.True(attribute.Required);
    }

    [Fact]
    public void JobParameterAttribute_DefaultValue_ShouldBeSettable()
    {
        // Arrange
        var attribute = new JobParameterAttribute("TestParam", typeof(string));
        var defaultValue = "default";

        // Act
        attribute.DefaultValue = defaultValue;

        // Assert
        Assert.Equal(defaultValue, attribute.DefaultValue);
    }

    [Fact]
    public void JobParameterAttribute_UnsupportedType_ErrorMessage_ShouldIncludeAllSupportedTypes()
    {
        // Act
        var exception = Assert.Throws<ArgumentException>(() => 
            new JobParameterAttribute("TestParam", typeof(decimal)));

        // Assert
        var message = exception.Message;
        Assert.Contains("Decimal", message);
        Assert.Contains("Supported types are:", message);
        
        // Check that all supported types are mentioned
        var supportedTypeNames = JobParameterAttribute.SupportedTypes
            .Concat(JobParameterAttribute.SupportedNullableTypes)
            .Select(t => t.Name)
            .Distinct()
            .OrderBy(n => n);

        foreach (var typeName in supportedTypeNames)
        {
            Assert.Contains(typeName, message);
        }
    }

    private static object GetDefaultValueForType(Type type)
    {
        return type switch
        {
            var t when t == typeof(string) => "default",
            var t when t == typeof(char) => 'A',
            var t when t == typeof(int) => 42,
            var t when t == typeof(long) => 42L,
            var t when t == typeof(double) => 3.14,
            var t when t == typeof(DateTime) => DateTime.Now,
            var t when t == typeof(TimeOnly) => TimeOnly.FromDateTime(DateTime.Now),
            var t when t == typeof(DateOnly) => DateOnly.FromDateTime(DateTime.Now),
            var t when t == typeof(Guid) => Guid.NewGuid(),
            var t when t == typeof(char?) => (char?)'A',
            var t when t == typeof(int?) => (int?)42,
            var t when t == typeof(long?) => (long?)42L,
            var t when t == typeof(double?) => (double?)3.14,
            var t when t == typeof(DateTime?) => (DateTime?)DateTime.Now,
            var t when t == typeof(TimeOnly?) => (TimeOnly?)TimeOnly.FromDateTime(DateTime.Now),
            var t when t == typeof(DateOnly?) => (DateOnly?)DateOnly.FromDateTime(DateTime.Now),
            var t when t == typeof(Guid?) => (Guid?)Guid.NewGuid(),
            _ => throw new ArgumentException($"Unsupported type for default value: {type.Name}")
        };
    }
} 