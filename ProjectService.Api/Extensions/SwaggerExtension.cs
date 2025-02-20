using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

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
            
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter '{token}'",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            };
    
            options.AddSecurityDefinition("Bearer", securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            };

            options.AddSecurityRequirement(securityRequirement);
            
            // options.OperationFilter<SecurityRequirementsOperationFilter>();
            
            // options.EnableAnnotations();
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