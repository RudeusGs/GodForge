namespace GodForge.Application.Common.Interfaces;

public interface IAdminSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
