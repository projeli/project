using AutoMapper;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Application.Dtos;

namespace Projeli.ProjectService.Application.Profiles;

public class ProjectTagProfile : Profile
{
    public ProjectTagProfile()
    {
        CreateMap<ProjectTag, ProjectTagDto>();
        CreateMap<ProjectTagDto, ProjectTag>();
        
        CreateMap<string, ProjectTagDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src));
    }
}