using Microsoft.EntityFrameworkCore;
using Projeli.ProjectService.Infrastructure.Database;
using Projeli.Shared.Infrastructure.Exceptions;

namespace Projeli.ProjectService.Api.Extensions;

public static class DatabaseExtension
{
    public static void AddProjectServiceDatabase(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration["Database:ConnectionString"];
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new MissingEnvironmentVariableException("Database:ConnectionString");
        }

        services.AddDbContext<ProjectServiceDbContext>(options =>
        {
            options.UseNpgsql(connectionString, builder => { builder.MigrationsAssembly("Projeli.ProjectService.Api"); });
            
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
            }
        });
    }

    public static void UseProjectServiceDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ProjectServiceDbContext>();
        if (database.Database.GetPendingMigrations().Any())
        {
            database.Database.Migrate();
        }
    }
}