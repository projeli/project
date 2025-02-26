namespace Projeli.ProjectService.Domain.Models;

[Flags]
public enum ProjectMemberPermissions : ulong
{
    None = 0,
    EditProject = (ulong) 1 << 0,
    PublishProject = (ulong) 1 << 1,
    ManageLinks = (ulong) 1 << 2,
    // Reserved (3 - 10) for future project-level permissions

    AddProjectMembers = (ulong) 1 << 11,
    EditProjectMemberRoles = (ulong) 1 << 12,
    EditProjectMemberPermissions = (ulong) 1 << 13,
    // Reserved (14 - 19) for future member-level permissions
    DeleteProjectMembers = (ulong) 1 << 20,
    
    DeleteProject = (ulong) 1 << 63,
    All = ulong.MaxValue
}