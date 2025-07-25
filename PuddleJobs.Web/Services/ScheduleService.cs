using PuddleJobs.Core;
using PuddleJobs.Core.DTOs;
using System.Net.Http.Json;

namespace PuddleJobs.Web.Services;

public class ScheduleService(IHttpClientFactory factory)
{
    private HttpClient Client => factory.CreateClient("PuddleJobsAPI");

    public async Task<List<ScheduleDto>> GetSchedulesAsync()
    {
        return await Client.GetFromJsonAsync<List<ScheduleDto>>("api/schedules") ?? [];
    }

    public async Task<ScheduleDto?> GetScheduleAsync(int id)
    {
        return await Client.GetFromJsonAsync<ScheduleDto>($"api/schedules/{id}");
    }

    public async Task<List<DateTime>> GetNextExecutionsAsync(int id, int count = 5)
    {
        return await Client.GetFromJsonAsync<List<DateTime>>($"api/schedules/{id}/next-executions?count={count}") ?? [];
    }

    public async Task<CronValidationResult?> ValidateCronAsync(string cronExpression)
    {
        var response = await Client.PostAsJsonAsync("api/schedules/validate-cron", cronExpression);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CronValidationResult>();
        
        return null;
    }

    public async Task<ScheduleDto?> CreateScheduleAsync(CreateScheduleDto dto)
    {
        var response = await Client.PostAsJsonAsync("api/schedules", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ScheduleDto>();
        return null;
    }

    public async Task<ScheduleDto?> UpdateScheduleAsync(int id, UpdateScheduleDto dto)
    {
        var response = await Client.PutAsJsonAsync($"api/schedules/{id}", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ScheduleDto>();
       
        return null;
    }

    public async Task<bool> DeleteScheduleAsync(int id)
    {
        var response = await Client.DeleteAsync($"api/schedules/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PauseScheduleAsync(int id)
    {
        var response = await Client.PostAsync($"api/schedules/{id}/pause", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResumeScheduleAsync(int id)
    {
        var response = await Client.PostAsync($"api/schedules/{id}/resume", null);
        return response.IsSuccessStatusCode;
    }
}
