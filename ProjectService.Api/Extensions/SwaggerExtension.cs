using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace ProjectService.Api.Extensions;

public static class SwaggerExtension
{
    public static void AddProjectServiceSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            OpenApiInfo info = new()
            {
                Title = "Project Service API",
                Version = "v1",
            };
            
            options.SwaggerDoc("v1", info);
            
            info.Version = "v2";
            options.SwaggerDoc("v2", info);
            
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
    
            options.OperationFilter<SecurityRequirementsOperationFilter>();
            
            options.EnableAnnotations();
        });
    }
    
    public static void UseProjectServiceSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/swagger/v1/swagger.json", "v1");
                options.SwaggerEndpoint($"/swagger/v2/swagger.json", "v2");
            }
        );
    }
}