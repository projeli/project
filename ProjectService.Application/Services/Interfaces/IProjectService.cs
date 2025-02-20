using ProjectService.Application.Dtos;
using ProjectService.Domain.Results;

namespace ProjectService.Application.Services.Interfaces;

public interface IProjectService
{
    Task<IResult<ProjectDto?>> GetById(Ulid id);
    Task<IResult<ProjectDto?>> GetBySlug(string slug);
    Task<IResult<ProjectDto?>> Create(ProjectDto projectDto, string userId);
}