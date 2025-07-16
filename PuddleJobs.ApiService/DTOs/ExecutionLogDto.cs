using System;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.DTOs;

public class ExecutionLogDto
{
    public int JobId { get; set; }
    public long FireInstanceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;

    public static ExecutionLogDto Create(ExecutionLog log)
    {
        return new ExecutionLogDto
        {
            JobId = log.JobId,
            FireInstanceId = log.FireInstanceId,
            StartTime = log.StartTime,
            EndTime = log.EndTime,
            Status = log.Status
        };
    }
}