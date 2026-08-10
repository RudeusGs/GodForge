using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Organizations.Commands.TransferOrganizationOwnership;
using Moq;

namespace GodForge.UnitTests.Application.Organizations.Commands;

public sealed class TransferOrganizationOwnershipCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithUndefinedNumericRetainedRole_ReturnsValidationBeforeTransaction()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new TransferOrganizationOwnershipCommandHandler(
            Mock.Of<IOrganizationRepository>(),
            Mock.Of<IOrganizationMemberRepository>(),
            Mock.Of<IProjectMemberRepository>(),
            unitOfWork.Object,
            Mock.Of<IUserRepository>(),
            Mock.Of<IAuditWriter>(),
            Mock.Of<IClock>());
        var request = new TransferOrganizationOwnershipCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "999",
            1);

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_ERROR", result.Error?.Code);
        unitOfWork.Verify(
            work => work.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
