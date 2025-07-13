using System;
using System.Globalization;
using PuddleJobs.ApiService.Helpers;
using Xunit;

namespace PuddleJobs.Tests.Helpers;

public class JobParameterHelperTests
{
    [Fact]
    public void ConvertJobParameterValue_StringType_ReturnsString()
    {
        // Arrange
        var value = "test string";
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(string).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void ConvertJobParameterValue_CharType_ReturnsChar()
    {
        // Arrange
        var value = "A";
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(char).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal('A', result);
    }

    [Fact]
    public void ConvertJobParameterValue_IntType_ReturnsInt()
    {
        // Arrange
        var value = "42";
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(int).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ConvertJobParameterValue_LongType_ReturnsLong()
    {
        // Arrange
        var value = "9223372036854775807";
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(long).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(9223372036854775807L, result);
    }

    [Fact]
    public void ConvertJobParameterValue_DoubleType_ReturnsDouble()
    {
        // Arrange
        var value = "3.14159";
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(double).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(3.14159, result);
    }

    [Fact]
    public void ConvertJobParameterValue_DateTimeType_ReturnsDateTime()
    {
        // Arrange
        var value = "2023-12-25T10:30:00";
        var expected = DateTime.Parse("2023-12-25T10:30:00");
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(DateTime).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertJobParameterValue_TimeOnlyType_ReturnsTimeOnly()
    {
        // Arrange
        var value = "10:30:45";
        var expected = TimeOnly.Parse("10:30:45");
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(TimeOnly).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertJobParameterValue_DateOnlyType_ReturnsDateOnly()
    {
        // Arrange
        var value = "2023-12-25";
        var expected = DateOnly.Parse("2023-12-25");
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(DateOnly).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertJobParameterValue_GuidType_ReturnsGuid()
    {
        // Arrange
        var value = "12345678-1234-1234-1234-123456789012";
        var expected = Guid.Parse("12345678-1234-1234-1234-123456789012");
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(Guid).AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(typeof(char?))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(TimeOnly?))]
    [InlineData(typeof(DateOnly?))]
    [InlineData(typeof(Guid?))]
    public void ConvertJobParameterValue_NullableTypes_ReturnCorrectValues(Type nullableType)
    {
        // Arrange
        var value = GetTestValueForType(nullableType);
        var expected = GetExpectedValueForType(nullableType);
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, nullableType.AssemblyQualifiedName!);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ConvertJobParameterValue_NullOrEmpty_ReturnsDefault(string? value)
    {
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, typeof(int).AssemblyQualifiedName!);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertJobParameterValue_InvalidInt_ThrowsException()
    {
        // Arrange
        var value = "not_an_int";
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JobParameterHelper.ConvertJobParameterValue(value, typeof(int).AssemblyQualifiedName!));
        
        Assert.Contains("Could not convert", exception.Message);
        Assert.Contains("not_an_int", exception.Message);
        Assert.Contains("Int32", exception.Message);
    }

    [Fact]
    public void ConvertJobParameterValue_InvalidGuid_ThrowsException()
    {
        // Arrange
        var value = "not_a_guid";
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JobParameterHelper.ConvertJobParameterValue(value, typeof(Guid).AssemblyQualifiedName!));
        
        Assert.Contains("Could not convert", exception.Message);
        Assert.Contains("not_a_guid", exception.Message);
        Assert.Contains("Guid", exception.Message);
    }

    [Fact]
    public void ConvertJobParameterValue_UnsupportedType_ThrowsException()
    {
        // Arrange
        var value = "test";
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JobParameterHelper.ConvertJobParameterValue(value, typeof(decimal).AssemblyQualifiedName!));
        
        Assert.Contains("Unsupported type", exception.Message);
        Assert.Contains("Decimal", exception.Message);
    }

    [Fact]
    public void ConvertJobParameterValue_StringOverload_ValidType_ReturnsConvertedValue()
    {
        // Arrange
        var value = "42";
        var targetType = typeof(int).AssemblyQualifiedName!;
        
        // Act
        var result = JobParameterHelper.ConvertJobParameterValue(value, targetType);
        
        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ConvertJobParameterValue_StringOverload_UnknownType_ThrowsException()
    {
        // Arrange
        var value = "test";
        var targetType = "NonExistentType, NonExistentAssembly";
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JobParameterHelper.ConvertJobParameterValue(value, targetType));
        
        Assert.Contains("Unknown type", exception.Message);
        Assert.Contains("NonExistentType", exception.Message);
    }

    [Fact]
    public void ConvertJobParameterValue_StringOverload_NullType_ThrowsException()
    {
        // Arrange
        var value = "test";
        string? targetType = null;
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            JobParameterHelper.ConvertJobParameterValue(value, targetType!));
        
        Assert.Contains("Unknown type", exception.Message);
    }

    private static string GetTestValueForType(Type type)
    {
        return type switch
        {
            var t when t == typeof(char?) => "A",
            var t when t == typeof(int?) => "42",
            var t when t == typeof(long?) => "9223372036854775807",
            var t when t == typeof(double?) => "3.14159",
            var t when t == typeof(DateTime?) => "2023-12-25T10:30:00",
            var t when t == typeof(TimeOnly?) => "10:30:45",
            var t when t == typeof(DateOnly?) => "2023-12-25",
            var t when t == typeof(Guid?) => "12345678-1234-1234-1234-123456789012",
            _ => throw new ArgumentException($"Unsupported type for test value: {type.Name}")
        };
    }

    private static object GetExpectedValueForType(Type type)
    {
        return type switch
        {
            var t when t == typeof(char?) => 'A',
            var t when t == typeof(int?) => 42,
            var t when t == typeof(long?) => 9223372036854775807L,
            var t when t == typeof(double?) => 3.14159,
            var t when t == typeof(DateTime?) => DateTime.Parse("2023-12-25T10:30:00"),
            var t when t == typeof(TimeOnly?) => TimeOnly.Parse("10:30:45"),
            var t when t == typeof(DateOnly?) => DateOnly.Parse("2023-12-25"),
            var t when t == typeof(Guid?) => Guid.Parse("12345678-1234-1234-1234-123456789012"),
            _ => throw new ArgumentException($"Unsupported type for expected value: {type.Name}")
        };
    }
} 