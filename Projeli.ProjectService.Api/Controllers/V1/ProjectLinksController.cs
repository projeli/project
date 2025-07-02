using Microsoft.AspNetCore.Mvc;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Models.Requests;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.Shared.Infrastructure.Extensions;

namespace Projeli.ProjectService.Api.Controllers.V1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("v1/projects/{id}/links")]
public class ProjectLinksController(
    IProjectLinkService projectLinkService
) : BaseController
{
    [HttpPut]
    public async Task<IActionResult> UpdateProjectLinks([FromRoute] Ulid id,
        [FromBody] UpdateProjectLinksRequest request)
    {
        var updatedProjectResult = await projectLinkService.UpdateLinks(id, request.Links.Select(link =>
            new ProjectLinkDto
            {
                Id = Ulid.NewUlid(),
                ProjectId = id,
                Name = link.Name,
                Url = link.Url,
                Type = link.Type,
                Order = link.Order,
            }).ToList(), User.GetId());

        return HandleResult(updatedProjectResult);
    }
}