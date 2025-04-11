using System.Reflection;
using Projeli.ProjectService.Api.Extensions;
using Projeli.ProjectService.Application.Profiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProjectServiceCors(builder.Configuration, builder.Environment);
builder.Services.AddProjectServiceSwagger();
builder.Services.AddProjectServiceServices();
builder.Services.AddProjectServiceRepositories();
builder.Services.AddControllers().AddProjectServiceJson();
builder.Services.AddProjectServiceDatabase(builder.Configuration, builder.Environment);
builder.Services.AddProjectServiceAuthentication(builder.Configuration, builder.Environment);
builder.Services.UseProjectServiceRabbitMq(builder.Configuration);
builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(ProjectProfile)));
builder.Services.AddProjectServiceOpenTelemetry(builder.Logging, builder.Configuration);

var app = builder.Build();

app.UseProjectServiceMiddleware();
app.MapControllers();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseProjectServiceSwagger();
}

app.UseProjectServiceCors();
app.UseProjectServiceAuthentication();
app.UseProjectServiceDatabase();
app.UseProjectServiceOpenTelemetry();

app.Run();