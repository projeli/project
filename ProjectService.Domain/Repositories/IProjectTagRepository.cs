using ProjectService.Domain.Models;

namespace ProjectService.Domain.Repositories;

public interface IProjectTagRepository
{
    Task<List<ProjectTag>> GetByTags(List<string> tags);
}