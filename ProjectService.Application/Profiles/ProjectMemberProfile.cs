using AutoMapper;
using ProjectService.Application.Dtos;
using ProjectService.Domain.Models;

namespace ProjectService.Application.Profiles;

public class ProjectMemberProfile : Profile
{
    public ProjectMemberProfile()
    {
        CreateMap<ProjectMember, ProjectMemberDto>();
        CreateMap<ProjectMemberDto, ProjectMember>();
    }
}