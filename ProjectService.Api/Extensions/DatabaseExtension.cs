using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using ProjectService.Api.Exceptions;
using ProjectService.Infrastructure.Converters;
using ProjectService.Infrastructure.Database;

namespace ProjectService.Api.Extensions;

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
            options.UseNpgsql(connectionString, builder => { builder.MigrationsAssembly("ProjectService.Api"); });
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