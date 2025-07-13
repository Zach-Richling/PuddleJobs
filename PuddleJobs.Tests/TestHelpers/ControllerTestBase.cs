using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace PuddleJobs.Tests.TestHelpers;

public abstract class ControllerTestBase
{
    protected readonly Mock<ILogger> MockLogger;

    protected ControllerTestBase()
    {
        MockLogger = new Mock<ILogger>();
    }

    protected static void AssertOkResult<T>(ActionResult<T> result, out T value)
    {
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        value = Assert.IsType<T>(okResult.Value);
    }

    protected static void AssertCreatedAtActionResult<T>(ActionResult<T> result, string expectedActionName, object expectedRouteValue, out T value)
    {
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(expectedActionName, createdAtActionResult.ActionName);
        Assert.Equal(expectedRouteValue, createdAtActionResult.RouteValues?["id"]);
        value = Assert.IsType<T>(createdAtActionResult.Value);
    }

    protected static void AssertBadRequestResult(ActionResult result, string expectedErrorMessage)
    {
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(expectedErrorMessage, badRequestResult.Value?.ToString());
    }

    protected static void AssertBadRequestResult<T>(ActionResult<T> result, string expectedErrorMessage)
    {
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains(expectedErrorMessage, badRequestResult.Value?.ToString());
    }

    protected static void AssertNotFoundResult(ActionResult result)
    {
        Assert.IsType<NotFoundResult>(result);
    }

    protected static void AssertNotFoundResult<T>(ActionResult<T> result)
    {
        Assert.IsType<NotFoundResult>(result.Result);
    }

    protected static void AssertNoContentResult(ActionResult result)
    {
        Assert.IsType<NoContentResult>(result);
    }
} 