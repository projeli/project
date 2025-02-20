using Microsoft.EntityFrameworkCore;
using ProjectService.Api.Exceptions;
using ProjectService.Infrastructure.Database;

namespace ProjectService.Api.Extensions;

public static class DatabaseExtension
{
    public static void AddDatabase(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            //Use SQLite in development
            services.AddDbContext<ProjectServiceDbContext>(options =>
            {
                options.UseSqlite("Data Source=projectservice.db");
            });
        }
        else
        {
            var connectionString = configuration["Database:ConnectionString"];
        
            if (string.IsNullOrEmpty(connectionString)) 
            {
                throw new MissingEnvironmentVariableException("Database:ConnectionString");
            }
            
            services.AddDbContext<ProjectServiceDbContext>(options =>
            {
                options.UseNpgsql(connectionString, builder =>
                {
                    builder.MigrationsAssembly("Atla.API");
                });
            });
        }
    }

    public static void UseDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ProjectServiceDbContext>();
        if (database.Database.GetPendingMigrations().Any())
        {
            database.Database.Migrate();
        }
    }
}