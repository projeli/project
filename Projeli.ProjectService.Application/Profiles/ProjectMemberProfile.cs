using AutoMapper;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Application.Dtos;

namespace Projeli.ProjectService.Application.Profiles;

public class ProjectMemberProfile : Profile
{
    public ProjectMemberProfile()
    {
        CreateMap<ProjectMember, ProjectMemberDto>();
        CreateMap<ProjectMemberDto, ProjectMember>();
    }
}