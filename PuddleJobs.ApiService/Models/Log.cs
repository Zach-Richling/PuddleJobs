using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuddleJobs.ApiService.Models;

public class Log
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? Message { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? MessageTemplate { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? Level { get; set; }

    public DateTime? TimeStamp { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? Exception { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? LogEvent { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? ClassName { get; set; }

    public long? FireInstanceId { get; set; }

    public ExecutionLog? ExecutionLog { get; set; }
} 