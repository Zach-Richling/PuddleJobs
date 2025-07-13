using Microsoft.AspNetCore.Mvc;
using PuddleJobs.ApiService.DTOs;
using PuddleJobs.ApiService.Models;
using PuddleJobs.ApiService.Services;

namespace PuddleJobs.ApiService.Controllers;

/// <summary>
/// Manages job schedules
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;

    public SchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Gets all schedules
    /// </summary>
    /// <returns>List of all schedules</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedules()
    {
        var schedules = await _scheduleService.GetAllSchedulesAsync();
        return Ok(schedules);
    }

    /// <summary>
    /// Gets a specific schedule by ID
    /// </summary>
    /// <param name="id">Schedule ID</param>
    /// <returns>Schedule details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(int id)
    {
        var schedule = await _scheduleService.GetScheduleByIdAsync(id);
        if (schedule == null)
            return NotFound();

        return Ok(schedule);
    }

    /// <summary>
    /// Gets the next execution times for a schedule
    /// </summary>
    /// <param name="id">Schedule ID</param>
    /// <param name="count">Number of next executions to return (default: 5)</param>
    /// <returns>List of next execution times</returns>
    [HttpGet("{id}/next-executions")]
    public ActionResult<IEnumerable<DateTime>> GetNextExecutions(int id, [FromQuery] int count = 5)
    {
        var nextTimes = _scheduleService.GetNextExecutionTimes(id, count);
        return Ok(nextTimes);
    }

    /// <summary>
    /// Validates a Cron expression
    /// </summary>
    /// <param name="cronExpression">The Cron expression to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-cron")]
    public ActionResult<CronValidationResult> ValidateCron([FromBody] string cronExpression)
    {
        var result = _scheduleService.ValidateCronExpression(cronExpression);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new schedule
    /// </summary>
    /// <param name="dto">Schedule creation data</param>
    /// <returns>Created schedule</returns>
    [HttpPost]
    public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleDto dto)
    {
        try
        {
            var schedule = await _scheduleService.CreateScheduleAsync(dto);
            return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates a schedule
    /// </summary>
    /// <param name="id">Schedule ID</param>
    /// <param name="dto">Updated schedule data</param>
    /// <returns>Updated schedule</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ScheduleDto>> UpdateSchedule(int id, UpdateScheduleDto dto)
    {
        try
        {
            var schedule = await _scheduleService.UpdateScheduleAsync(id, dto);
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a schedule (soft delete)
    /// </summary>
    /// <param name="id">Schedule ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSchedule(int id)
    {
        var deleted = await _scheduleService.DeleteScheduleAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Pauses a schedule (pauses all associated job triggers)
    /// </summary>
    /// <param name="id">Schedule ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/pause")]
    public async Task<ActionResult> PauseSchedule(int id)
    {
        try
        {
            await _scheduleService.PauseScheduleAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Resumes a schedule (resumes all associated job triggers)
    /// </summary>
    /// <param name="id">Schedule ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/resume")]
    public async Task<ActionResult> ResumeSchedule(int id)
    {
        try
        {
            await _scheduleService.ResumeScheduleAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
} 