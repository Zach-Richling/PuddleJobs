using PuddleJobs.ApiService.Services;
using Quartz;

namespace PuddleJobs.ApiService.Jobs;

public class PuddleJob(IJobExecutionService jobExecutionService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await jobExecutionService.ExecuteJobAsync(context);
    }
} 