using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Domain.Models;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services.Interfaces;

public interface IProjectLinkService
{
    Task<IResult<List<ProjectLinkDto>?>> UpdateLinks(Ulid id, List<ProjectLinkDto> links, string userId);
}