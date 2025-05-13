using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projeli.ProjectService.Application.Models.Requests;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.Shared.Infrastructure.Extensions;

namespace Projeli.ProjectService.Api.Controllers.V1;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("v1/projects/{projectId}/members")]
public class ProjectMembersController(IProjectMemberService projectMemberService) : BaseController
{
 
    [HttpGet]
    public async Task<IActionResult> GetProjectMembers([FromRoute] Ulid projectId)
    {
        var result = await projectMemberService.Get(projectId, User.TryGetId());
        return HandleResult(result);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddProjectMember([FromRoute] Ulid projectId, [FromBody] CreateProjectMemberRequest request)
    {
        var result = await projectMemberService.Add(projectId, request.UserId, User.GetId());
        return HandleResult(result);
    }
    
    [HttpDelete("{userId}")]
    [Authorize]
    public async Task<IActionResult> RemoveProjectMember([FromRoute] Ulid projectId, [FromRoute] string userId)
    {
        var result = await projectMemberService.Delete(projectId, userId, User.GetId());
        return HandleResult(result);
    }
    
    [HttpPut("{userId}/permissions")]
    [Authorize]
    public async Task<IActionResult> UpdateProjectMemberPermissions([FromRoute] Ulid projectId, [FromRoute] string userId, [FromBody] UpdateProjectMemberPermissionsRequest request)
    {
        var result = await projectMemberService.UpdatePermissions(projectId, userId, request.Permissions, User.GetId());
        return HandleResult(result);
    }
}