using AutoMapper;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Models.Requests;

namespace Projeli.ProjectService.Application.Profiles;

public class ProjectProfile : Profile
{
    public ProjectProfile()
    {
        // Domain
        CreateMap<Project, ProjectDto>();
        CreateMap<ProjectDto, Project>();

        // Results

        // Requests
        CreateMap<CreateProjectRequest, ProjectDto>();
        CreateMap<UpdateProjectRequest, ProjectDto>();

        // Responses
    }
}