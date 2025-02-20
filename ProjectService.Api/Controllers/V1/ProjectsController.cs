using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectService.Application.Dtos;
using ProjectService.Application.Models.Requests;
using ProjectService.Application.Services.Interfaces;
using ProjectService.Domain.Models;
using ProjectService.Domain.Results;
using ProjectService.Infrastructure.Extensions;

namespace ProjectService.Api.Controllers.V1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("v1/projects")]
public class ProjectsController(IProjectService projectService, IMapper mapper) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string query = "",
        [FromQuery] ProjectOrder order = ProjectOrder.Relevance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userId = null
    )
    {
        var projectsResult = await projectService.Get(query, order, page, pageSize, userId, User.TryGetId());

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

        return HandleResult(mapper.Map<Result<ProjectDto>>(result));
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
}