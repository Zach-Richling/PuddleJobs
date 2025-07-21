using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuddleJobs.ApiService.Data;
using PuddleJobs.Core.DTOs;
using PuddleJobs.ApiService.Helpers;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LogsController(JobSchedulerDbContext context) : ControllerBase
{
    private readonly JobSchedulerDbContext _context = context;

    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<IEnumerable<ExecutionLogDto>>> GetExecutionLogsForJob(int jobId)
    {
        var logs = _context.ExecutionLogs
            .Where(l => l.JobId == jobId)
            .OrderByDescending(l => l.StartTime)
            .Select(ExecutionLog.CreateDto);
        
        return Ok(logs);
    }

    [HttpGet("job/{jobId}/latest")]
    public async Task<ActionResult<ExecutionLogDto>> GetLatestExecutionLogForJob(int jobId)
    {
        var log = await _context.ExecutionLogs
            .Where(l => l.JobId == jobId)
            .OrderByDescending(l => l.StartTime)
            .FirstOrDefaultAsync();

        if (log == null)
            return NotFound();

        return Ok(ExecutionLog.CreateDto(log));
    }

    [HttpGet("execution/{fireInstanceId}")]
    public async Task<ActionResult<IEnumerable<LogDto>>> GetLogsByFireInstanceId(long fireInstanceId)
    {
        var logs = _context.Logs
            .Where(l => l.FireInstanceId == fireInstanceId &&
            l.ClassName == nameof(PuddleJob))
            .Select(Log.CreateDto);

        return Ok(logs);
    }
} 