using System;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.DTOs;

public class LogDto
{
    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Level { get; set; }
    public DateTime? TimeStamp { get; set; }
    public string? Exception { get; set; }
    public string? LogEvent { get; set; }
    public string? ClassName { get; set; }
    public long? FireInstanceId { get; set; }

    public static LogDto Create(Log log)
    {
        return new LogDto
        {
            Message = log.Message,
            MessageTemplate = log.MessageTemplate,
            Level = log.Level,
            TimeStamp = log.TimeStamp,
            Exception = log.Exception,
            LogEvent = log.LogEvent,
            ClassName = log.ClassName,
            FireInstanceId = log.FireInstanceId
        };
    }
} 