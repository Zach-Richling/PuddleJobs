using PuddleJobs.ApiService.Services;
using Quartz;

namespace PuddleJobs.ApiService.Jobs;

public class PuddleJob : IJob
{
    private readonly IJobExecutionService _jobExecutionService;

    public PuddleJob(IJobExecutionService jobExecutionService)
    {
        _jobExecutionService = jobExecutionService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _jobExecutionService.ExecuteJobAsync(context);
    }
} 