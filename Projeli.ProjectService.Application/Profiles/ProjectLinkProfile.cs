using AutoMapper;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Application.Dtos;

namespace Projeli.ProjectService.Application.Profiles;

public class ProjectLinkProfile : Profile
{
    public ProjectLinkProfile()
    {
        CreateMap<ProjectLink, ProjectLinkDto>();
        CreateMap<ProjectLinkDto, ProjectLink>();
    }
}