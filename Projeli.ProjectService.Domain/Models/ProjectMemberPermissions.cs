namespace Projeli.ProjectService.Domain.Models;

[Flags]
public enum ProjectMemberPermissions : ulong
{
    None = 0,
    EditProject = (ulong)1 << 0,
    PublishProject = (ulong)1 << 1,
    ArchiveProject = (ulong)1 << 2,
    ManageLinks = (ulong)1 << 3,
    ManageTags = (ulong)1 << 4,
    // Reserved (5 - 10) for future project-level permissions

    AddProjectMembers = (ulong)1 << 11,
    EditProjectMemberRoles = (ulong)1 << 12,
    EditProjectMemberPermissions = (ulong)1 << 13,

    // Reserved (14 - 19) for future member-level permissions
    DeleteProjectMembers = (ulong)1 << 20,

    DeleteProject = (ulong)1 << 63,
    All = ulong.MaxValue
}