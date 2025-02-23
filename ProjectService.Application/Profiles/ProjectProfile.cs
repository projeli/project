using AutoMapper;
using ProjectService.Application.Dtos;
using ProjectService.Application.Models.Requests;
using ProjectService.Domain.Models;

namespace ProjectService.Application.Profiles;

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

        // Responses
    }
}