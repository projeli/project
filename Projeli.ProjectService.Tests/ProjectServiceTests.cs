using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Domain.Models;
using Projeli.ProjectService.Domain.Repositories;
using Projeli.Shared.Application.Exceptions.Http;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Tests;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly Application.Services.ProjectService _service;

    public ProjectServiceTests()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        Mock<IBusRepository> busRepositoryMock = new();
        _mapper =
            new MapperConfiguration(cfg => cfg.AddMaps(typeof(Application.Profiles.ProjectProfile))).CreateMapper();
        _service = new Application.Services.ProjectService(_repositoryMock.Object, busRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Get_ReturnsPagedResult_WhenProjectsExist()
    {
        // Arrange
        var projects = new List<Project> { new() { Id = Ulid.NewUlid(), Name = "Test" } };
        var pagedResult = new PagedResult<Project>
        {
            Data = projects,
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
        _repositoryMock.Setup(r => r.Get("", ProjectOrder.Relevance, null, null, 1, 20, null, null))
            .ReturnsAsync(pagedResult);
        _mapper.Map<List<ProjectDto>>(projects);

        // Act
        var result = await _service.Get("", ProjectOrder.Relevance, null, null, 1, 20);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!);
    }

    [Fact]
    public async Task GetById_ReturnsProject_WhenExists()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var project = new Project { Id = id, Name = "Test" };
        _repositoryMock.Setup(r => r.GetById(id, null, false)).ReturnsAsync(project);
        _mapper.Map<ProjectDto>(project);

        // Act
        var result = await _service.GetById(id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(id, result.Data!.Id);
    }

    [Fact]
    public async Task GetBySlug_ReturnsProject_WhenExists()
    {
        // Arrange
        var slug = "test";
        var project = new Project { Slug = slug, Name = "Test" };
        _repositoryMock.Setup(r => r.GetBySlug(slug, null, false)).ReturnsAsync(project);
        _mapper.Map<ProjectDto>(project);

        // Act
        var result = await _service.GetBySlug(slug);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(slug, result.Data!.Slug);
    }

    [Fact]
    public async Task Create_ReturnsProject_WhenValid()
    {
        // Arrange
        var imageFile = new FormFile(new MemoryStream(), 0, 0, "file", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        var projectDto = new ProjectDto
            { Name = "New Project", Slug = "new-project", Category = ProjectCategory.Technology };
        var project = _mapper.Map<Project>(projectDto);
        _repositoryMock.Setup(r => r.Create(It.IsAny<Project>())).ReturnsAsync(project);

        // Act
        var result = await _service.Create(projectDto, imageFile, "user123");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateDetails_ReturnsUpdatedProject_WhenAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Name = "Old",
            Slug = "old",
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        var projectDto = new ProjectDto { Name = "New", Slug = "new", Category = ProjectCategory.Technology };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.GetBySlug("new", null, true)).ReturnsAsync((Project?)null);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Project>())).ReturnsAsync(existingProject);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateDetails(id, projectDto, "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New", result.Data!.Name);
    }

    [Fact]
    public async Task UpdateDetails_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateDetails(id, new ProjectDto(), "user123"));
    }

    [Fact]
    public async Task UpdateDetails_ReturnsError_WhenNoName()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateDetails(id, new ProjectDto
        {
            Slug = "new",
            Category = ProjectCategory.Adventure
        }, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("name", result.Errors.Keys);
    }

    [Fact]
    public async Task UpdateDetails_ReturnsError_WhenNoSlug()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateDetails(id, new ProjectDto
        {
            Name = "New",
            Category = ProjectCategory.Adventure
        }, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("slug", result.Errors.Keys);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("#Invalid")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task UpdateDetails_ReturnsError_WhenInvalidName(string invalidName)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateDetails(id, new ProjectDto
        {
            Name = invalidName,
            Category = ProjectCategory.Adventure
        }, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("name", result.Errors.Keys);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("ABC")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("#Invalid")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("test")]
    public async Task UpdateDetails_ReturnsError_WhenInvalidSlug(string invalidSlug)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.GetBySlug("test", null, true))
            .ReturnsAsync(new Project { Name = "Test", Slug = "test" });

        // Act
        var result = await _service.UpdateDetails(id, new ProjectDto
        {
            Name = "New",
            Slug = invalidSlug,
            Category = ProjectCategory.Adventure
        }, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("slug", result.Errors.Keys);
    }
    
    [Theory]
    [InlineData("a")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("This is an invalid character test#")]
    public async Task UpdateDetails_ReturnsError_WhenInvalidSummary(string invalidSummary)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateDetails(id, new ProjectDto
        {
            Name = "New",
            Slug = "new",
            Summary = invalidSummary,
            Category = ProjectCategory.Adventure
        }, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("summary", result.Errors.Keys);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(38)]
    [InlineData(255)]
    public async Task UpdateDetails_ReturnsError_WhenInvalidCategory(int invalidCategory)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateDetails(id, new ProjectDto
        {
            Name = "New",
            Slug = "new",
            Summary = "This is a valid summary for the project.",
            Category = (ProjectCategory) invalidCategory
        }, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("category", result.Errors.Keys);
    }

    [Fact]
    public async Task UpdateContent_ReturnsUpdatedProject_WhenAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Content = "Old",
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Project>())).ReturnsAsync(existingProject);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateContent(id, "New", "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New", result.Data!.Content);
    }

    [Fact]
    public async Task UpdateTags_ReturnsUpdatedProject_WhenAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Name = "Test",
            Slug = "test",
            Category = ProjectCategory.Adventure,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Project>())).ReturnsAsync(existingProject);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateTags(id, ["tag"], "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Tags);
        Assert.Equal("tag", result.Data.Tags[0].Name);
    }
    
    [Theory]
    [InlineData("a")]
    [InlineData("AB")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("aaaaaaaa", "bbbbbbbb", "cccccccc", "dddddddd", "eeeeeeee", "ffffffff")]
    public async Task UpdateTags_ReturnsError_WhenInvalidTag(params string[] invalidTag)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateTags(id, invalidTag, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("tags", result.Errors.Keys);
    }
    
    [Fact]
    public async Task GetByUserId_ReturnsProjects_WhenUserHasProjects()
    {
        // Arrange
        var userId = "user123";
        var projects = new List<Project>
        {
            new Project { Id = Ulid.NewUlid(), Name = "Project1", Members = [new ProjectMember { UserId = userId, IsOwner = true }] },
            new Project { Id = Ulid.NewUlid(), Name = "Project2", Members = [new ProjectMember { UserId = userId, IsOwner = false }] }
        };
        _repositoryMock.Setup(r => r.GetByUserId(userId)).ReturnsAsync(projects);
        _mapper.Map<List<ProjectDto>>(projects);

        // Act
        var result = await _service.GetByUserId(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal("Project1", result.Data[0].Name);
        Assert.Equal("Project2", result.Data[1].Name);
    }

    [Fact]
    public async Task GetByUserId_ReturnsEmptyList_WhenUserHasNoProjects()
    {
        // Arrange
        const string userId = "user123";
        _repositoryMock.Setup(r => r.GetByUserId(userId)).ReturnsAsync([]);

        // Act
        var result = await _service.GetByUserId(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data!);
    }

    [Theory]
    [InlineData(ProjectStatus.Published)]
    [InlineData(ProjectStatus.Archived)]
    public async Task UpdateStatus_ReturnsUpdatedProject_WhenAuthorizedAndValidStatus(ProjectStatus status)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Status = status == ProjectStatus.Published ? ProjectStatus.Draft : ProjectStatus.Published,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        var updatedProject = new Project
        {
            Id = id,
            Status = status,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.UpdateStatus(id, status)).ReturnsAsync(updatedProject);

        // Act
        var result = await _service.UpdateStatus(id, status, "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(status, result.Data!.Status);
    }

    [Fact]
    public async Task UpdateStatus_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Status = ProjectStatus.Draft,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateStatus(id, ProjectStatus.Published, "user123"));
    }

    [Theory]
    [InlineData(ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Review)]
    public async Task UpdateStatus_ReturnsError_WhenInvalidStatusTransition(ProjectStatus status)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Status = ProjectStatus.Published,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateStatus(id, status, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot set status to", result.Message);
    }

    [Fact]
    public async Task UpdateOwnership_ReturnsUpdatedProject_WhenAuthorizedAndNewOwnerExists()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members =
            [
                new ProjectMember { Id = Ulid.NewUlid(), UserId = "user123", IsOwner = true },
                new ProjectMember { Id = Ulid.NewUlid(), UserId = "newOwner", IsOwner = false }
            ]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.UpdateOwnership(id, It.IsAny<Ulid>(), It.IsAny<Ulid>(), It.IsAny<ProjectMemberPermissions>(), It.IsAny<ProjectMemberPermissions>()))
            .ReturnsAsync(existingProject);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateOwnership(id, "newOwner", "user123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task UpdateOwnership_ThrowsForbidden_WhenNotOwner()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateOwnership(id, "newOwner", "user123"));
    }

    [Fact]
    public async Task UpdateOwnership_ReturnsError_WhenNewOwnerNotInProject()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { Id = Ulid.NewUlid(), UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateOwnership(id, "newOwner", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("New owner does not exist in the project", result.Message);
    }

    [Fact]
    public async Task UpdateImageUrl_ReturnsUpdatedProject_WhenProjectExists()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project { Id = id, Name = "Test" };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.UpdateImageUrl(id, "path/to/image")).ReturnsAsync(existingProject);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateImageUrl(id, "path/to/image", "user123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task UpdateImageUrl_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        var id = Ulid.NewUlid();
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync((Project?)null);

        // Act
        var result = await _service.UpdateImageUrl(id, "path/to/image", "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Message);
    }

    [Fact]
    public async Task UpdateImage_ReturnsUpdatedProject_WhenAuthorizedAndValidImage()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        var imageFile = new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "file", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.UpdateImage(id, imageFile, "user123")).Returns(Task.CompletedTask);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateImage(id, imageFile, "user123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task UpdateImage_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        var imageFile = new FormFile(new MemoryStream(new byte[1024]), 0, 1024, "file", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateImage(id, imageFile, "user123"));
    }

    [Theory]
    [InlineData(512, "image/png", "Image must be at least 1KB")]
    [InlineData(3 * 1024 * 1024, "image/png", "Image must be at most 2MB")]
    [InlineData(1024, "image/bmp", "Image must be a JPEG, PNG, GIF or WEBP")]
    public async Task UpdateImage_ReturnsError_WhenInvalidImage(long fileSize, string contentType, string expectedError)
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        var imageFile = new FormFile(new MemoryStream(new byte[fileSize]), 0, fileSize, "file", "test")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act
        var result = await _service.UpdateImage(id, imageFile, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(expectedError, result.Message);
    }

    [Fact]
    public async Task Delete_ReturnsDeletedProject_WhenAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.Delete(id)).ReturnsAsync(true);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.Delete(id, "user123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Delete_ThrowsForbidden_WhenNotAuthorized()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var existingProject = new Project
        {
            Id = id,
            Members = [new ProjectMember { UserId = "otherUser", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.Delete(id, "user123"));
    }

    [Fact]
    public async Task Delete_ReturnsError_WhenProjectNotFound()
    {
        // Arrange
        var id = Ulid.NewUlid();
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync((Project?)null);

        // Act
        var result = await _service.Delete(id, "user123");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("Not found", result.Message);
    }
}