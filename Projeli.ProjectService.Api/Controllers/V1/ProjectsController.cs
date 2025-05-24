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
        [FromQuery] string? userId = null,
        [FromQuery] List<Ulid>? ids = null
    )
    {
        if (ids is not null && ids.Count > 0)
        {
            var projectsByIdsResult = await projectService.GetByIds(ids, User.TryGetId());
            return HandleResult(projectsByIdsResult);
        }

        var projectsResult =
            await projectService.Get(query, order, categories, tags, page, pageSize, userId, User.TryGetId());

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
    public async Task<IActionResult> CreateProject([FromForm] CreateProjectRequest request)
    {
        var projectDto = mapper.Map<ProjectDto>(request);

        var createdProjectResult = await projectService.Create(projectDto, request.Image, User.GetId());

        return createdProjectResult.Success
            ? CreatedAtAction(nameof(GetProject), new { id = createdProjectResult.Data?.Id }, createdProjectResult)
            : HandleResult(createdProjectResult);
    }

    [HttpPut("{id}/details")]
    [Authorize]
    public async Task<IActionResult> UpdateProject([FromRoute] Ulid id, [FromBody] UpdateProjectDetailsRequest request)
    {
        var projectDto = mapper.Map<ProjectDto>(request);

        var updatedProjectResult = await projectService.UpdateDetails(id, projectDto, User.GetId());

        return HandleResult(updatedProjectResult);
    }

    [HttpPut("{id}/content")]
    [Authorize]
    public async Task<IActionResult> UpdateProjectContent([FromRoute] Ulid id,
        [FromBody] UpdateProjectContentRequest request)
    {
        var updatedProjectResult = await projectService.UpdateContent(id, request.Content, User.GetId());

        return HandleResult(updatedProjectResult);
    }

    [HttpPut("{id}/tags")]
    [Authorize]
    public async Task<IActionResult> UpdateProjectTags([FromRoute] Ulid id, [FromBody] UpdateProjectTagsRequest request)
    {
        var updatedProjectResult = await projectService.UpdateTags(id, request.Tags, User.GetId());

        return HandleResult(updatedProjectResult);
    }

    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateProjectStatus([FromRoute] Ulid id,
        [FromBody] UpdateProjectStatusRequest request)
    {
        var updatedProjectResult = await projectService.UpdateStatus(id, request.Status, User.GetId());

        return HandleResult(updatedProjectResult);
    }

    [HttpPut("{id}/ownership")]
    [Authorize]
    public async Task<IActionResult> UpdateProjectOwnership([FromRoute] Ulid id,
        [FromBody] UpdateProjectOwnershipRequest request)
    {
        var updatedProjectResult = await projectService.UpdateOwnership(id, request.UserId, User.GetId());

        return HandleResult(updatedProjectResult);
    }

    [HttpPut("{id}/image")]
    [Authorize]
    public async Task<IActionResult> UpdateProjectImage([FromRoute] Ulid id, [FromForm] IFormFile image)
    {
        var updatedProjectResult = await projectService.UpdateImage(id, image, User.GetId());

        return HandleResult(updatedProjectResult);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteProject([FromRoute] Ulid id)
    {
        var deletedProjectResult = await projectService.Delete(id, User.GetId());

        return HandleResult(deletedProjectResult);
    }
}