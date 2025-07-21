using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Services;

namespace PuddleJobs.ApiService.Controllers;

/// <summary>
/// Manages jobs and their parameters
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IJobParameterService _jobParameterService;

    public JobsController(IJobService jobService, IJobParameterService jobParameterService)
    {
        _jobService = jobService;
        _jobParameterService = jobParameterService;
    }

    /// <summary>
    /// Gets all jobs
    /// </summary>
    /// <returns>List of all jobs</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobDto>>> GetJobs()
    {
        var jobs = await _jobService.GetAllJobsAsync();
        return Ok(jobs);
    }

    /// <summary>
    /// Gets a specific job by ID
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Job details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<JobDto>> GetJob(int id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null)
            return NotFound();

        return Ok(job);
    }

    /// <summary>
    /// Gets parameter values for a job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>Parameter values</returns>
    [HttpGet("{id}/parameter-values")]
    public async Task<ActionResult<IEnumerable<JobParameterDto>>> GetJobParameterValues(int id)
    {
        try
        {
            var parameters = await _jobParameterService.GetJobParameterValuesAsync(id);
            return Ok(parameters);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Sets multiple parameter values for a job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="parameters">Parameter values</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}/parameter-values")]
    public async Task<ActionResult> SetJobParameterValues(int id, [FromBody] IEnumerable<JobParameterValueDto> parameters)
    {
        try
        {
            await _jobParameterService.SetJobParameterValuesAsync(id, parameters);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Creates a new job
    /// </summary>
    /// <param name="dto">Job creation data</param>
    /// <returns>Created job</returns>
    [HttpPost]
    public async Task<ActionResult<JobDto>> CreateJob(CreateJobDto dto)
    {
        try
        {
            var job = await _jobService.CreateJobAsync(dto);
            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates a job
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="dto">Updated job data</param>
    /// <returns>Updated job</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<JobDto>> UpdateJob(int id, UpdateJobDto dto)
    {
        try
        {
            var job = await _jobService.UpdateJobAsync(id, dto);
            return Ok(job);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a job (soft delete)
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteJob(int id)
    {
        var deleted = await _jobService.DeleteJobAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
} 