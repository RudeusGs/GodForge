using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Repositories.Commands.LinkRepository;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using Moq;

namespace GodForge.UnitTests.Application.Repositories.Commands;

public class LinkRepositoryCommandHandlerTests
{
    private readonly Mock<IGitRepositoryRepository> _repositoriesMock;
    private readonly Mock<IAuthorizationService> _authorizationMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IActivityWriter> _activityWriterMock;
    private readonly Mock<IClock> _clockMock;
    private readonly LinkRepositoryCommandHandler _handler;
    private readonly DateTimeOffset _now;

    public LinkRepositoryCommandHandlerTests()
    {
        _repositoriesMock = new Mock<IGitRepositoryRepository>();
        _authorizationMock = new Mock<IAuthorizationService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _activityWriterMock = new Mock<IActivityWriter>();
        _clockMock = new Mock<IClock>();
        
        _handler = new LinkRepositoryCommandHandler(
            _repositoriesMock.Object,
            _authorizationMock.Object,
            _unitOfWorkMock.Object,
            _activityWriterMock.Object,
            _clockMock.Object);

        _now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);
        _clockMock.Setup(c => c.UtcNow).Returns(_now);
    }

    [Fact]
    public async Task Handle_WithoutPermission_ReturnsForbidden()
    {
        var request = new LinkRepositoryCommand(Guid.NewGuid(), "https://github.com/a/b", "GitHub", "main", null, true, Guid.NewGuid(), "corr-1");
        _authorizationMock.Setup(a => a.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("SECURITY_FORBIDDEN", result.Error!.Code);
        _repositoriesMock.Verify(r => r.AddAsync(It.IsAny<GitRepository>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryAlreadyExists_ReturnsConflict()
    {
        var request = new LinkRepositoryCommand(Guid.NewGuid(), "https://github.com/a/b", "GitHub", "main", null, true, Guid.NewGuid(), "corr-1");
        _authorizationMock.Setup(a => a.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var existingRepo = GitRepository.CreateLinked(request.ProjectId, "https://old.url", GitProvider.GitHub, "main", null, true, _now);
        _repositoriesMock.Setup(r => r.GetByProjectIdAsync(request.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRepo);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("REPOSITORY_ALREADY_CONNECTED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidProvider_ReturnsValidationFailed()
    {
        var request = new LinkRepositoryCommand(Guid.NewGuid(), "https://github.com/a/b", "InvalidProvider", "main", null, true, Guid.NewGuid(), "corr-1");
        _authorizationMock.Setup(a => a.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoriesMock.Setup(r => r.GetByProjectIdAsync(request.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitRepository?)null);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("REPOSITORY_PROVIDER_INVALID", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WithForgejoProvider_ReturnsValidationFailed()
    {
        var request = new LinkRepositoryCommand(Guid.NewGuid(), "https://forgejo.com/a/b", "Forgejo", "main", null, true, Guid.NewGuid(), "corr-1");
        _authorizationMock.Setup(a => a.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoriesMock.Setup(r => r.GetByProjectIdAsync(request.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitRepository?)null);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("REPOSITORY_PROVIDER_INVALID", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesRepositoryAndReturnsSuccess()
    {
        var request = new LinkRepositoryCommand(Guid.NewGuid(), "https://github.com/a/b.git", "GitHub", "master", "ext-id-123", true, Guid.NewGuid(), "corr-123");
        _authorizationMock.Setup(a => a.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoriesMock.Setup(r => r.GetByProjectIdAsync(request.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GitRepository?)null);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.RemoteUrl, result.Value!.CloneUrl);
        Assert.Equal(request.DefaultBranch, result.Value.DefaultBranch);
        Assert.Equal(GitProvider.GitHub.ToString(), result.Value.Provider);
        
        _repositoriesMock.Verify(r => r.AddAsync(It.Is<GitRepository>(repo => 
            repo.ProjectId == request.ProjectId &&
            repo.RemoteUrl == request.RemoteUrl &&
            repo.Provider == GitProvider.GitHub &&
            repo.Mode == RepositoryMode.ExternalLinked
        ), It.IsAny<CancellationToken>()), Times.Once);

        _activityWriterMock.Verify(a => a.WriteAsync(
            request.ProjectId,
            request.ActorId,
            "repository.linked",
            "repository",
            It.IsAny<string>(),
            ActivityStatus.Succeeded,
            request.CorrelationId,
            null,
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
