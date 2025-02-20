using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectService.Api.Controllers;

[ApiController]
[Route("projects")]
public class ProjectsController : ControllerBase
{
    
    [HttpGet]
    [Authorize]
    public IActionResult GetProjects()
    {
        var user = User;
        return Ok(new[]
        {
            new { Id = 1, Name = "Project 1" },
            new { Id = 2, Name = "Project 2" },
            new { Id = 3, Name = "Project 3" }
        });
    }
}