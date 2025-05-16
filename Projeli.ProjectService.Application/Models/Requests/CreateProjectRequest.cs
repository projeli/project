using Microsoft.AspNetCore.Http;
using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Application.Models.Requests;

public class CreateProjectRequest
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public string? Summary { get; set; }
    public ProjectCategory? Category { get; set; }
    public IFormFile Image { get; set; }
}