using AutoMapper;
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
        _mapper =
            new MapperConfiguration(cfg => cfg.AddMaps(typeof(Application.Profiles.ProjectProfile))).CreateMapper();
        _service = new Application.Services.ProjectService(_repositoryMock.Object, _mapper);
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
        var projectDto = new ProjectDto
            { Name = "New Project", Slug = "new-project", Category = ProjectCategory.Technology };
        var project = _mapper.Map<Project>(projectDto);
        _repositoryMock.Setup(r => r.Create(It.IsAny<Project>())).ReturnsAsync(project);

        // Act
        var result = await _service.Create(projectDto, "user123");

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
            Members = [new ProjectMember { UserId = "user123", IsOwner = true }]
        };
        _repositoryMock.Setup(r => r.GetById(id, "user123", false)).ReturnsAsync(existingProject);
        _repositoryMock.Setup(r => r.Update(It.IsAny<Project>())).ReturnsAsync(existingProject);
        _mapper.Map<ProjectDto>(existingProject);

        // Act
        var result = await _service.UpdateTags(id, ["tag1"], "user123");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Tags);
        Assert.Equal("tag1", result.Data.Tags[0].Name);
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
}