using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PuddleJobs.Web.Services;

namespace PuddleJobs.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddMudServices();

            var puddleJobsApi = builder.Configuration.GetValue<string>("PuddleJobsApi")!;

            builder.Services.AddHttpClient("PuddleJobsAPI", client =>
            {
                client.BaseAddress = new Uri(puddleJobsApi);
            }).AddHttpMessageHandler(services =>
            {
                var handler = services.GetRequiredService<AuthorizationMessageHandler>()
                    .ConfigureHandler(authorizedUrls: [puddleJobsApi]);
                return handler;
            });

            builder.Services.AddOidcAuthentication(options =>
            {
                builder.Configuration.Bind("Keycloak", options.ProviderOptions);
            });

            builder.Services.AddScoped<JobService>();
            builder.Services.AddScoped<ScheduleService>();

            await builder.Build().RunAsync();
        }
    }
}
