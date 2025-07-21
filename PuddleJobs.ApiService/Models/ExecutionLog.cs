using PuddleJobs.Core.DTOs;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuddleJobs.ApiService.Models;

public class ExecutionLog
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Job")]
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;

    [Required]
    public long FireInstanceId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Running";

    public ICollection<Log> Logs { get; set; } = [];

    public static ExecutionLogDto CreateDto(ExecutionLog executionLog)
    {
        return new ExecutionLogDto
        {
            JobId = executionLog.JobId,
            FireInstanceId = executionLog.FireInstanceId,
            StartTime = executionLog.StartTime,
            EndTime = executionLog.EndTime,
            Status = executionLog.Status
        };
    }
} 