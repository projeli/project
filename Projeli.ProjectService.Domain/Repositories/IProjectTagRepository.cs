using Projeli.ProjectService.Domain.Models;

namespace Projeli.ProjectService.Domain.Repositories;

public interface IProjectTagRepository
{
    Task<List<ProjectTag>> GetByTags(List<string> tags);
}