using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Models.Requests;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.ProjectService.Domain.Models;
using Projeli.Shared.Domain.Results;
using Projeli.Shared.Infrastructure.Extensions;

namespace Projeli.ProjectService.Api.Controllers.V1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("v1/projects")]
public class ProjectsController(IProjectService projectService, IMapper mapper) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string query = "",
        [FromQuery] ProjectOrder order = ProjectOrder.Relevance,
        [FromQuery] List<ProjectCategory>? categories = null,
        [FromQuery] string[]? tags = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userId = null
    )
    {
        var projectsResult = await projectService.Get(query, order, categories, tags, page, pageSize, userId, User.TryGetId());

        return HandleResult(projectsResult);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject([FromRoute] string id)
    {
        IResult<ProjectDto?> result;
        if (Ulid.TryParse(id, out var ulid))
        {
            result = await projectService.GetById(ulid, User.TryGetId());
        }
        else
        {
            result = await projectService.GetBySlug(id, User.TryGetId());
        }

        return HandleResult(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var projectDto = mapper.Map<ProjectDto>(request);

        var createdProjectResult = await projectService.Create(projectDto, User.GetId());

        return createdProjectResult is { Success: true }
            ? CreatedAtAction(nameof(GetProject), new { id = createdProjectResult.Data!.Id }, createdProjectResult)
            : HandleResult(createdProjectResult);
    }
    
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateProject([FromRoute] Ulid id, [FromBody] UpdateProjectRequest request)
    {
        var projectDto = mapper.Map<ProjectDto>(request);

        var updatedProjectResult = await projectService.Update(id, projectDto, User.GetId());

        return HandleResult(updatedProjectResult);
    }
}