using AutoMapper;
using ProjectService.Application.Dtos;
using ProjectService.Domain.Models;

namespace ProjectService.Application.Profiles;

public class ProjectLinkProfile : Profile
{
    public ProjectLinkProfile()
    {
        CreateMap<ProjectLink, ProjectLinkDto>();
        CreateMap<ProjectLinkDto, ProjectLink>();
    }
}