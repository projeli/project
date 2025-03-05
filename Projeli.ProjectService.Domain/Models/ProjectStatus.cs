namespace Projeli.ProjectService.Domain.Models;

public enum ProjectStatus : ushort
{
    Draft = 0,
    Review = 1,
    Published = 2,
    Archived = 3
}