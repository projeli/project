﻿using Projeli.ProjectService.Domain.Models;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Domain.Repositories;

public interface IProjectRepository
{
    Task<PagedResult<Project>> Get(string query, ProjectOrder order, List<ProjectCategory>? categories, string[]? tags, int page, int pageSize, string? fromUserId = null, string? userId = null);
    Task<Project?> GetById(Ulid id, string? userId = null, bool force = false);
    Task<Project?> GetBySlug(string slug, string? userId = null, bool force = false);
    Task<Project?> Create(Project project);
    Task<Project?> Update(Project project);
    Task<Project?> UpdateStatus(Ulid id, ProjectStatus status);
    Task<bool> Delete(Ulid id);
}