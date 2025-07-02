using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Domain.Repositories;

public interface IProjectLinkRepository
{
    Task<List<ProjectLink>?> UpdateLinks(Ulid projectId, List<ProjectLink> links);
}