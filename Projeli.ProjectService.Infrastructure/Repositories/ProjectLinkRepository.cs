using Microsoft.EntityFrameworkCore;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.ProjectService.Infrastructure.Database;

namespace Projeli.ProjectService.Infrastructure.Repositories;

public class ProjectLinkRepository(ProjectServiceDbContext database) : IProjectLinkRepository
{
    public async Task<List<ProjectLink>?> UpdateLinks(Ulid projectId, List<ProjectLink> links)
    {
        var project = await database.Projects.Include(p => p.Links).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return null;

        var linksDict = links.ToDictionary(l => l.Id, l => l);

        var linksToRemove = project.Links
            .Where(l => !linksDict.ContainsKey(l.Id))
            .ToList();

        foreach (var link in linksToRemove)
        {
            project.Links.Remove(link);
        }

        foreach (var newLink in links)
        {
            var existingLink = project.Links.FirstOrDefault(l => l.Id == newLink.Id);
            if (existingLink == null)
            {
                project.Links.Add(newLink);
            }
            else
            {
                database.Entry(existingLink).CurrentValues.SetValues(newLink);
            }
        }

        var success = await database.SaveChangesAsync() > 0;

        return success ? project.Links : null;
    }
}