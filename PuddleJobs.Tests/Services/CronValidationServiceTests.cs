using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;
using Quartz;

namespace PuddleJobs.Tests.Services;

public class CronValidationServiceTests
{
    private readonly CronValidationService _service = new();

    #region IsValidCronExpression

    [Theory]
    [InlineData("0 0 * * * ?", true)]
    [InlineData("0 15 10 ? * *", true)]
    [InlineData("* * * * *", false)] // Invalid for Quartz
    [InlineData("", false)]
    [InlineData("invalid cron", false)]
    public void IsValidCronExpression_ReturnsExpectedResult(string cron, bool expected)
    {
        var result = _service.IsValidCronExpression(cron);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidCronExpression_ReturnsFalse_WhenNull()
    {
        var result = _service.IsValidCronExpression(null!);
        Assert.False(result);
    }

    #endregion

    #region ValidateCronExpression

    [Fact]
    public void ValidateCronExpression_ReturnsSuccess_WhenValid()
    {
        var cron = "0 0 * * * ?";
        var result = _service.ValidateCronExpression(cron);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateCronExpression_ReturnsFailure_WhenEmpty()
    {
        var result = _service.ValidateCronExpression("");
        Assert.False(result.IsValid);
        Assert.Equal("Cron expression cannot be empty or null", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCronExpression_ReturnsFailure_WhenNull()
    {
        var result = _service.ValidateCronExpression(null);
        Assert.False(result.IsValid);
        Assert.Equal("Cron expression cannot be empty or null", result.ErrorMessage);
    }

    [Fact]
    public void ValidateCronExpression_ReturnsFailure_WhenInvalid()
    {
        var cron = "invalid cron";
        var result = _service.ValidateCronExpression(cron);
        Assert.False(result.IsValid);
        Assert.StartsWith("Invalid Cron expression:", result.ErrorMessage);
    }

    #endregion

    #region GetNextExecutionTimes

    [Fact]
    public void GetNextExecutionTimes_ReturnsTimes_WhenValid()
    {
        var cron = "0 0 * * * ?"; // Every hour
        var result = _service.GetNextExecutionTimes(cron, 3).ToList();
        Assert.Equal(3, result.Count);
        Assert.True(result[1] > result[0]);
        Assert.True(result[2] > result[1]);
    }

    [Fact]
    public void GetNextExecutionTimes_ReturnsEmpty_WhenInvalid()
    {
        var cron = "invalid cron";
        var result = _service.GetNextExecutionTimes(cron, 3);
        Assert.Empty(result);
    }

    [Fact]
    public void GetNextExecutionTimes_ReturnsEmpty_WhenEmpty()
    {
        var result = _service.GetNextExecutionTimes("", 3);
        Assert.Empty(result);
    }

    #endregion
} 