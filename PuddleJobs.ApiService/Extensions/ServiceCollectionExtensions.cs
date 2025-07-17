using Microsoft.OpenApi.Models;

namespace PuddleJobs.ApiService.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(o => 
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PuddleJobs API",
                Version = "v1",
                Description = "A .NET job scheduler using Quartz.NET with assembly versioning and parameter management"
            });

            o.CustomSchemaIds(id => id.FullName!.Replace("+", "-"));
            o.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows() 
                { 
                    Implicit = new OpenApiOAuthFlow() 
                    { 
                        AuthorizationUrl = new Uri(configuration["Keycloak:AuthorizationUrl"]!),
                        Scopes = new Dictionary<string, string>() 
                        {
                            { "openid", "openid" },
                            { "profile", "profile" }
                        }
                    }
                }
            });

            var securityRequirement = new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme()
                    {
                        Reference = new OpenApiReference()
                        {
                            Id = "Keycloak",
                            Type = ReferenceType.SecurityScheme
                        },
                        In = ParameterLocation.Header,
                        Name = "Bearer",
                        Scheme = "Bearer"
                    },
                    []
                }
            };

            o.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}
