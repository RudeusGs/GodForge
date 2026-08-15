using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Organizations.Commands.UpdateOrganization;
using GodForge.Domain.Entities.Core;
using Moq;

namespace GodForge.UnitTests.Application.Organizations.Commands;

public sealed class UpdateOrganizationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithStaleVersion_ReturnsConcurrencyConflictWithoutMutatingOrSaving()
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var organization = Organization.Create("Original", "original", actorId, now);
        var membership = OrganizationMember.CreateOwner(organization.Id, actorId, now);
        var organizations = new Mock<IOrganizationRepository>();
        var members = new Mock<IOrganizationMemberRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        organizations.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        members.Setup(x => x.GetAsync(organization.Id, actorId, It.IsAny<CancellationToken>())).ReturnsAsync(membership);
        var handler = new UpdateOrganizationCommandHandler(
            organizations.Object,
            members.Object,
            Mock.Of<IProjectMemberRepository>(),
            unitOfWork.Object,
            Mock.Of<IAuditWriter>(),
            Mock.Of<IClock>());

        var result = await handler.Handle(
            new UpdateOrganizationCommand(actorId, organization.Id, "Changed", "changed", organization.Version + 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("CONCURRENCY_CONFLICT", result.Error?.Code);
        Assert.Equal("Original", organization.Name);
        Assert.Equal("original", organization.Slug);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
