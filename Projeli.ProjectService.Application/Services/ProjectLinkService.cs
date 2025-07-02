using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AutoMapper;
using MassTransit.Util;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Application.Services;

public partial class ProjectLinkService(
    IProjectRepository projectRepository,
    IProjectLinkRepository projectLinkRepository,
    IMapper mapper)
    : IProjectLinkService
{
    public async Task<IResult<List<ProjectLinkDto>?>> UpdateLinks(Ulid id, List<ProjectLinkDto> links, string userId)
    {
        var existingProject = await projectRepository.GetById(id, userId);
        if (existingProject is null) return Result<List<ProjectLinkDto>>.NotFound();

        var member = existingProject.Members.FirstOrDefault(member => member.UserId == userId);
        if (member is null || (!member.IsOwner && !member.Permissions.HasFlag(ProjectMemberPermissions.ManageLinks)))
        {
            throw new ForbiddenException("You do not have permission to edit the project links.");
        }

        var newLinks = mapper.Map<List<ProjectLink>>(links);

        var validationResult = ValidateLinks(newLinks);
        if (validationResult.Failed) return validationResult;

        var updatedLinks = await projectLinkRepository.UpdateLinks(id, newLinks);

        return updatedLinks is not null
            ? new Result<List<ProjectLinkDto>>(mapper.Map<List<ProjectLinkDto>>(updatedLinks))
            : Result<List<ProjectLinkDto>>.Fail("Failed to update project");
    }

    private static IResult<List<ProjectLinkDto>?> ValidateLinks(List<ProjectLink> links)
    {
        var errors = new Dictionary<string, string[]>();

        switch (links.Count)
        {
            case > 10:
                errors.Add("links", ["A project may have at most 10 links"]);
                break;
            case > 0:
            {
                for (var i = 0; i < links.Count; i++)
                {
                    var link = links[i];
                
                    if (link.Name.Length < 2)
                    {
                        errors.Add($"links.{i}.name", ["Link must be at least 2 characters long"]);
                        continue;
                    }

                    if (link.Name.Length > 16)
                    {
                        errors.Add($"links.{i}.name", ["Link must be at most 24 characters long"]);
                        continue;
                    }

                    if (!LinkRegex().IsMatch(link.Name))
                    {
                        errors.Add($"links.{i}.name", ["Link contains invalid characters"]);
                    }

                    if (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
                    {
                        if (uri.Scheme != Uri.UriSchemeHttps)
                        {
                            errors.Add($"links.{i}.url", ["Link must use https protocol"]);
                        }
                    }
                    else
                    {
                        errors.Add($"links.{i}.url", ["Link must be a valid URL"]);
                    }

                    if (link.Url.Length > 256)
                    {
                        errors.Add($"links.{i}.url", ["Link must be at most 256 characters long"]);
                    }
                }
                break;
            }
        }

        return errors.Count > 0
            ? Result<List<ProjectLinkDto>?>.ValidationFail(errors)
            : new Result<List<ProjectLinkDto>?>(null);
    }

    [GeneratedRegex(@"^[\w\s\.,!?'""()&+\-*/\\:;@%<>=|{}\[\]^~]{2,16}$")]
    public static partial Regex LinkRegex();
}