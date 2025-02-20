using System.Reflection;
using ProjectService.Api.Extensions;
using ProjectService.Application.Profiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProjectServiceCors(builder.Configuration, builder.Environment);
builder.Services.AddProjectServiceSwagger();
builder.Services.AddProjectServiceServices();
builder.Services.AddProjectServiceRepositories();
builder.Services.AddControllers();
builder.Services.AddProjectServiceDatabase(builder.Configuration, builder.Environment);
builder.Services.AddProjectServiceAuthentication(builder.Configuration);
builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(ProjectProfile)));

var app = builder.Build();

app.UseAtlaMiddleware();
app.MapControllers();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseProjectServiceSwagger();
}

app.UseProjectServiceCors();
app.UseProjectServiceAuthentication();
app.UseProjectServiceDatabase();

app.Run();