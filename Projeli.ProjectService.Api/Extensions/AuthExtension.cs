using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Projeli.ProjectService.Api.Extensions;

public static class AuthExtension
{
    public static void AddProjectServiceAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                x.Authority = configuration["Clerk:Authority"];
                x.RequireHttpsMetadata = environment.IsProduction();
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    NameClaimType = ClaimTypes.NameIdentifier 
                };
                x.Events = new JwtBearerEvents
                {
                    // Additional validation for AZP claim
                    OnTokenValidated = context =>
                    {
                        var azp = context.Principal?.FindFirstValue("azp");
                        if (string.IsNullOrEmpty(azp) || !azp.Equals(configuration["Clerk:AuthorizedParty"]))
                            context.Fail("AZP Claim is invalid or missing");

                        return Task.CompletedTask;
                    }
                };
            });
    }

    public static void UseProjectServiceAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
}