using System.Reflection;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace GodForge.UnitTests.Infrastructure.Persistence;

public sealed class UnitOfWorkTests
{
    private static readonly FieldInfo TransactionField = typeof(UnitOfWork).GetField(
        "_transaction",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Fact]
    public async Task RollbackTransactionAsync_WithCancelledRequestToken_UsesNonCancellableCleanup()
    {
        await using var context = CreateContext();
        var transaction = new Mock<IDbContextTransaction>();
        transaction
            .Setup(value => value.RollbackAsync(CancellationToken.None))
            .Returns(Task.CompletedTask);
        transaction
            .Setup(value => value.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        var unitOfWork = CreateUnitOfWork(context, transaction.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await unitOfWork.RollbackTransactionAsync(cancellation.Token);

        transaction.Verify(value => value.RollbackAsync(CancellationToken.None), Times.Once);
        transaction.Verify(value => value.DisposeAsync(), Times.Once);
        Assert.Null(GetTransaction(unitOfWork));
    }

    [Fact]
    public async Task CommitTransactionAsync_WhenCommitFails_DisposesAndClearsTransaction()
    {
        await using var context = CreateContext();
        var transaction = new Mock<IDbContextTransaction>();
        transaction
            .Setup(value => value.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        transaction
            .Setup(value => value.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
        var unitOfWork = CreateUnitOfWork(context, transaction.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            unitOfWork.CommitTransactionAsync(CancellationToken.None));

        transaction.Verify(value => value.DisposeAsync(), Times.Once);
        Assert.Null(GetTransaction(unitOfWork));
    }

    private static GodForgeDbContext CreateContext()
        => new(new DbContextOptionsBuilder<GodForgeDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .UseSnakeCaseNamingConvention()
            .Options);

    private static UnitOfWork CreateUnitOfWork(
        GodForgeDbContext context,
        IDbContextTransaction transaction)
    {
        var unitOfWork = new UnitOfWork(context);
        TransactionField.SetValue(unitOfWork, transaction);
        return unitOfWork;
    }

    private static IDbContextTransaction? GetTransaction(UnitOfWork unitOfWork)
        => (IDbContextTransaction?)TransactionField.GetValue(unitOfWork);
}
