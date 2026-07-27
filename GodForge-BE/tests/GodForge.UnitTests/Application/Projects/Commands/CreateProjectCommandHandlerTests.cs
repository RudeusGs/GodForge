using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Projects.Commands.CreateProject;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Moq;
using Xunit;

namespace GodForge.UnitTests.Application.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_NameConflict_ReturnsConflictError()
    {
        // Arrange
        var mockProjectRepo = new Mock<IProjectRepository>();
        var mockMemberRepo = new Mock<IProjectMemberRepository>();
        var mockActivityWriter = new Mock<IActivityWriter>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockClock = new Mock<IClock>();

        var actorId = Guid.NewGuid();
        mockProjectRepo.Setup(r => r.NameExistsAsync(actorId, "Conflict Project", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateProjectCommandHandler(
            mockProjectRepo.Object,
            mockMemberRepo.Object,
            mockActivityWriter.Object,
            mockUow.Object,
            mockClock.Object);

        var command = new CreateProjectCommand("Conflict Project", "Desc", "Private", actorId, "corr-id");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PROJECT_NAME_EXISTS", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesProjectAndMember()
    {
        // Arrange
        var mockProjectRepo = new Mock<IProjectRepository>();
        var mockMemberRepo = new Mock<IProjectMemberRepository>();
        var mockActivityWriter = new Mock<IActivityWriter>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockClock = new Mock<IClock>();

        var actorId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        mockClock.Setup(c => c.UtcNow).Returns(now);

        mockProjectRepo.Setup(r => r.NameExistsAsync(actorId, "New Project", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        mockProjectRepo.Setup(r => r.SlugExistsAsync("new-project", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateProjectCommandHandler(
            mockProjectRepo.Object,
            mockMemberRepo.Object,
            mockActivityWriter.Object,
            mockUow.Object,
            mockClock.Object);

        var command = new CreateProjectCommand("New Project", "Desc", "Private", actorId, "corr-id");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("New Project", result.Value.Name);

        mockProjectRepo.Verify(r => r.AddAsync(It.Is<Project>(p => p.Name == "New Project" && p.Status == ProjectStatus.Draft), It.IsAny<CancellationToken>()), Times.Once);
        mockMemberRepo.Verify(r => r.AddAsync(It.Is<ProjectMember>(m => m.UserId == actorId && m.Role == ProjectRole.ProjectOwner), It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

