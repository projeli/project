using AutoMapper;
using ProjectService.Application.Dtos;
using ProjectService.Domain.Models;

namespace ProjectService.Application.Profiles;

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