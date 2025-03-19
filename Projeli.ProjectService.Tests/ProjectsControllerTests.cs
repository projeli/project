using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeli.ProjectService.Api.Controllers.V1;
using Projeli.ProjectService.Application.Dtos;
using Projeli.ProjectService.Application.Models.Requests;
using Projeli.ProjectService.Application.Profiles;
using Projeli.ProjectService.Application.Services.Interfaces;
using Projeli.ProjectService.Domain.Models;
using Projeli.Shared.Domain.Results;

namespace Projeli.ProjectService.Tests;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly IMapper _mapper;
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _mapper = new MapperConfiguration(cfg => cfg.AddMaps(Assembly.GetAssembly(typeof(ProjectProfile)))).CreateMapper();
        _controller = new ProjectsController(_projectServiceMock.Object, _mapper);
    }

    [Fact]
    public async Task GetProjects_ReturnsOkResult_WhenProjectsExist()
    {
        // Arrange
        var projectsResult = new PagedResult<ProjectDto>
        {
            Data = [new ProjectDto { Id = Ulid.NewUlid(), Name = "Test Project" }],
            Success = true,
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
        _projectServiceMock.Setup(s => s.Get("", ProjectOrder.Relevance, null, null, 1, 20, null, null))
            .ReturnsAsync(projectsResult);

        // Act
        var result = await _controller.GetProjects();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<ProjectDto>>(okResult.Value);
        Assert.True(returnValue.Success);
        Assert.Single(returnValue.Data!);
    }

    [Fact]
    public async Task GetProject_ById_ReturnsOkResult_WhenProjectExists()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var projectResult = new Result<ProjectDto?>(new ProjectDto { Id = projectId, Name = "Test" });
        _projectServiceMock.Setup(s => s.GetById(projectId, null, false)).ReturnsAsync(projectResult);

        // Act
        var result = await _controller.GetProject(projectId.ToString());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<Result<ProjectDto?>>(okResult.Value);
        Assert.True(returnValue.Success);
        Assert.Equal(projectId, returnValue.Data!.Id);
    }

    [Fact]
    public async Task GetProject_BySlug_ReturnsOkResult_WhenProjectExists()
    {
        // Arrange
        const string slug = "test-slug";
        var projectResult = new Result<ProjectDto?>(new ProjectDto { Slug = slug, Name = "Test" });
        _projectServiceMock.Setup(s => s.GetBySlug(slug, null, false)).ReturnsAsync(projectResult);
        
        // Act
        var result = await _controller.GetProject(slug);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<Result<ProjectDto?>>(okResult.Value);
        Assert.True(returnValue.Success);
        Assert.Equal(slug, returnValue.Data!.Slug);
    }

    [Fact]
    public async Task CreateProject_ReturnsCreatedResult_WhenSuccessful()
    {
        // Arrange
        var request = new CreateProjectRequest { Name = "New Project", Slug = "new-project", Summary = null, Category = ProjectCategory.Adventure };
        var projectDto = _mapper.Map<ProjectDto>(request);
        var result = new Result<ProjectDto?>(projectDto);
        _projectServiceMock.Setup(s => s.Create(It.IsAny<ProjectDto>(), "user123")).ReturnsAsync(result);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "user123")
                ]))
            }
        };
        
        // Act
        var actionResult = await _controller.CreateProject(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult);
        var returnValue = Assert.IsType<Result<ProjectDto?>>(createdResult.Value);
        Assert.Equal(projectDto.Id.ToString(), createdResult.RouteValues!["id"]!.ToString());
        Assert.True(returnValue.Success);
    }

    [Fact]
    public async Task UpdateProjectDetails_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var request = new UpdateProjectDetailsRequest { Name = "Updated Project", Slug = "updated-project" };
        var projectDto = _mapper.Map<ProjectDto>(request);
        var result = new Result<ProjectDto?>(projectDto);
        _projectServiceMock.Setup(s => s.UpdateDetails(id, It.IsAny<ProjectDto>(), "user123")).ReturnsAsync(result);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "user123")
                ]))
            }
        };
        
        // Act
        var actionResult = await _controller.UpdateProject(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnValue = Assert.IsType<Result<ProjectDto?>>(okResult.Value);
        Assert.True(returnValue.Success);
    }

    [Fact]
    public async Task UpdateProjectContent_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var request = new UpdateProjectContentRequest { Content = "Updated content" };
        var result = new Result<ProjectDto?>(new ProjectDto { Id = id, Content = "Updated content" });
        _projectServiceMock.Setup(s => s.UpdateContent(id, request.Content, "user123")).ReturnsAsync(result);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "user123")
                ]))
            }
        };
        
        // Act
        var actionResult = await _controller.UpdateProjectContent(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnValue = Assert.IsType<Result<ProjectDto?>>(okResult.Value);
        Assert.True(returnValue.Success);
    }

    [Fact]
    public async Task UpdateProjectTags_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var id = Ulid.NewUlid();
        var request = new UpdateProjectTagsRequest { Tags = ["tag1", "tag2"] };
        var result = new Result<ProjectDto?>(new ProjectDto
            { Id = id, Tags = request.Tags.Select(t => new ProjectTagDto { Name = t }).ToList() });
        _projectServiceMock.Setup(s => s.UpdateTags(id, request.Tags, "user123")).ReturnsAsync(result);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "user123")
                ]))
            }
        };
        
        // Act
        var actionResult = await _controller.UpdateProjectTags(id, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnValue = Assert.IsType<Result<ProjectDto?>>(okResult.Value);
        Assert.True(returnValue.Success);
    }
}