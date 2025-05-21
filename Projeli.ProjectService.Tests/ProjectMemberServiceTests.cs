using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Services;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Domain.Results;
using Xunit;

namespace Projeli.ProjectService.Tests;

public class ProjectMemberServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<IProjectMemberRepository> _projectMemberRepositoryMock;
    private readonly ProjectMemberService _service;

    public ProjectMemberServiceTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _projectMemberRepositoryMock = new Mock<IProjectMemberRepository>();
        Mock<IBusRepository> busRepositoryMock = new();
        var mapper = new MapperConfiguration(cfg => cfg.AddMaps(typeof(Application.Profiles.ProjectProfile))).CreateMapper();
        _service = new ProjectMemberService(_projectRepositoryMock.Object, _projectMemberRepositoryMock.Object, busRepositoryMock.Object,  mapper);
    }

    [Fact]
    public async Task Get_ReturnsMembers_WhenProjectExists()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var members = new List<ProjectMember>
        {
            new() { Id = Ulid.NewUlid(), ProjectId = projectId, UserId = "user1", Role = "Member" },
            new() { Id = Ulid.NewUlid(), ProjectId = projectId, UserId = "user2", Role = "Admin" }
        };
        _projectMemberRepositoryMock.Setup(r => r.Get(projectId, null)).ReturnsAsync(members);

        // Act
        var result = await _service.Get(projectId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("Member", result.Data[0].Role);
        Assert.Equal("Admin", result.Data[1].Role);
    }

    [Fact]
    public async Task Get_ReturnsEmptyList_WhenNoMembers()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        _projectMemberRepositoryMock.Setup(r => r.Get(projectId, null)).ReturnsAsync(new List<ProjectMember>());

        // Act
        var result = await _service.Get(projectId);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task Add_ReturnsNewMember_WhenAuthorizedAndValid()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        var newMember = new ProjectMember
        {
            Id = Ulid.NewUlid(),
            ProjectId = projectId,
            UserId = "newUser",
            Role = "Member",
            Permissions = ProjectMemberPermissions.None,
            IsOwner = false
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);
        _projectMemberRepositoryMock.Setup(r => r.Add(projectId, It.IsAny<ProjectMember>(), It.IsAny<string>())).ReturnsAsync(newMember);

        // Act
        var result = await _service.Add(projectId, "newUser", "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("newUser", result.Data!.UserId);
        Assert.Equal("Member", result.Data.Role);
    }

    [Fact]
    public async Task Add_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.Add(projectId, "newUser", "user123"));
    }

    [Fact]
    public async Task Add_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync((Project?)null);

        // Act
        var result = await _service.Add(projectId, "newUser", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Message);
    }

    [Fact]
    public async Task Add_ReturnsError_WhenMemberLimitReached()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = Enumerable.Range(1, 20).Select(_ => new ProjectMember { UserId = Ulid.NewUlid().ToString(), IsOwner = false }).ToList()
        };
        existingProject.Members.Add(new ProjectMember { UserId = "user123", IsOwner = true });
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.Add(projectId, "newUser", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Project member limit reached", result.Message);
    }

    [Fact]
    public async Task Add_ReturnsError_WhenUserAlreadyMember()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }, new ProjectMember { UserId = "newUser" }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.Add(projectId, "newUser", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is already a member of the project", result.Message);
    }

    [Fact]
    public async Task Add_ReturnsError_WhenAddingSelf()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.Add(projectId, "user123", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is already a member of the project.", result.Message);
    }

    [Fact]
    public async Task UpdateRole_ReturnsUpdatedMember_WhenAuthorizedAndValidRole()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", IsOwner = true },
                new ProjectMember { Id = memberId, UserId = "targetUser", Role = "Member" }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);
        _projectMemberRepositoryMock.Setup(r => r.UpdateRole(projectId, memberId, "Admin")).ReturnsAsync(existingProject.Members[1]);

        // Act
        var result = await _service.UpdateRole(projectId, memberId, "Admin", "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Admin", result.Data!.Role);
    }

    [Fact]
    public async Task UpdateRole_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateRole(projectId, memberId, "Admin", "user123"));
    }

    [Fact]
    public async Task UpdateRole_ThrowsForbidden_WhenUpdatingOwnerRole()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", Permissions = ProjectMemberPermissions.EditProjectMemberRoles },
                new ProjectMember { Id = memberId, UserId = "owner", IsOwner = true, Role = "Owner" }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateRole(projectId, memberId, "Admin", "user123"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("ThisRoleIsWayTooLong")]
    [InlineData("#Invalid")]
    public async Task UpdateRole_ReturnsError_WhenInvalidRole(string invalidRole)
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", IsOwner = true },
                new ProjectMember { Id = memberId, UserId = "targetUser", Role = "Member" }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateRole(projectId, memberId, invalidRole, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("role", result.Errors.Keys);
    }

    [Fact]
    public async Task UpdateRole_ReturnsNotFound_WhenMemberDoesNotExist()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateRole(projectId, memberId, "Admin", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Message);
    }

    [Fact]
    public async Task UpdatePermissions_ReturnsUpdatedMember_WhenAuthorizedAndValidPermissions()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", IsOwner = true },
                new ProjectMember { Id = memberId, UserId = "targetUser", Permissions = ProjectMemberPermissions.None }
            ]
        };
        var newPermissions = ProjectMemberPermissions.EditProject | ProjectMemberPermissions.AddProjectMembers;
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);
        _projectMemberRepositoryMock.Setup(r => r.UpdatePermissions(projectId, memberId, newPermissions)).ReturnsAsync(existingProject.Members[1]);

        // Act
        var result = await _service.UpdatePermissions(projectId, memberId, newPermissions, "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(newPermissions, result.Data!.Permissions);
    }

    [Fact]
    public async Task UpdatePermissions_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdatePermissions(projectId, memberId, ProjectMemberPermissions.EditProject, "user123"));
    }

    [Fact]
    public async Task UpdatePermissions_ThrowsForbidden_WhenUpdatingOwnerPermissions()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", Permissions = ProjectMemberPermissions.EditProjectMemberPermissions },
                new ProjectMember { Id = memberId, UserId = "owner", IsOwner = true }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdatePermissions(projectId, memberId, ProjectMemberPermissions.None, "user123"));
    }

    [Fact]
    public async Task UpdatePermissions_ThrowsForbidden_WhenAddingUnauthorizedPermissions()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", Permissions = ProjectMemberPermissions.EditProject },
                new ProjectMember { Id = memberId, UserId = "targetUser", Permissions = ProjectMemberPermissions.None }
            ]
        };
        var newPermissions = ProjectMemberPermissions.AddProjectMembers;
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdatePermissions(projectId, memberId, newPermissions, "user123"));
    }

    [Fact]
    public async Task UpdatePermissions_ReturnsNotFound_WhenMemberDoesNotExist()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var memberId = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdatePermissions(projectId, memberId, ProjectMemberPermissions.EditProject, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Message);
    }

    [Fact]
    public async Task Delete_ReturnsDeletedMember_WhenAuthorized()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var targetUserId = "targetUser";
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", IsOwner = true },
                new ProjectMember { UserId = targetUserId, Role = "Member" }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);
        _projectMemberRepositoryMock.Setup(r => r.Delete(projectId, targetUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.Delete(projectId, targetUserId, "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(targetUserId, result.Data!.UserId);
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var targetUserId = "targetUser";
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.Delete(projectId, targetUserId, "user123"));
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_WhenOnlyMember()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var targetUserId = "user123";
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = targetUserId, IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, targetUserId, false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.Delete(projectId, targetUserId, targetUserId));
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_WhenDeletingOwner()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var targetUserId = "owner";
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", IsOwner = true },
                new ProjectMember { UserId = targetUserId, IsOwner = true }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.Delete(projectId, targetUserId, "user123"));
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenMemberDoesNotExist()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var targetUserId = "targetUser";
        var existingProject = new Project
        {
            Id = projectId,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.Delete(projectId, targetUserId, "user1234");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Message);
    }

    [Fact]
    public async Task Delete_SucceedsWithForce_WhenAuthorized()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var targetUserId = "targetUser";
        var existingProject = new Project
        {
            Id = projectId,
            Members = [
                new ProjectMember { UserId = "user123", IsOwner = true },
                new ProjectMember { UserId = targetUserId, Role = "Member" }
            ]
        };
        _projectRepositoryMock.Setup(r => r.GetById(projectId, null, true)).ReturnsAsync(existingProject);
        _projectMemberRepositoryMock.Setup(r => r.Delete(projectId, targetUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.Delete(projectId, targetUserId, null, true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(targetUserId, result.Data!.UserId);
    }
}