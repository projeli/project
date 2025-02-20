using ProjectService.Domain.Models;

namespace ProjectService.Domain.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetById(Ulid id);
    Task<Project?> GetBySlug(string slug);
    Task<Project?> Create(Project project);
}