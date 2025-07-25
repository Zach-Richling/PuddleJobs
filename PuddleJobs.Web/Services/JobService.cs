using PuddleJobs.Core.DTOs;
using System.Net.Http.Json;

namespace PuddleJobs.Web.Services
{
    public class JobService(IHttpClientFactory factory)
    {
        private HttpClient Client => factory.CreateClient("PuddleJobsAPI");

        public async Task<List<JobDto>> GetAllJobsAsync()
        {
            return await Client.GetFromJsonAsync<List<JobDto>>("api/jobs") ?? new();
        }

        public async Task<JobDto?> GetJobByIdAsync(int id)
        {
            return await Client.GetFromJsonAsync<JobDto>($"api/jobs/{id}");
        }

        public async Task<JobDto?> CreateJobAsync(CreateJobDto dto)
        {
            var response = await Client.PostAsJsonAsync("api/jobs", dto);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<JobDto>();
            return null;
        }

        public async Task<JobDto?> UpdateJobAsync(int id, UpdateJobDto dto)
        {
            var response = await Client.PutAsJsonAsync($"api/jobs/{id}", dto);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<JobDto>();
            return null;
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            var response = await Client.DeleteAsync($"api/jobs/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
