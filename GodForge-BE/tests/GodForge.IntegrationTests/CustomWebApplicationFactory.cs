using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using GodForge.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace GodForge.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "integration-test-only-signing-key-64-characters-minimum-000000000000",
                ["Jwt:Issuer"] = "GodForge.IntegrationTests",
                ["Jwt:Audience"] = "GodForge.IntegrationTests",
                ["OutboxEncryption:Key"] = "integration-test-only-outbox-encryption-key-64-characters-0000000000",
                ["Frontend:BaseUrl"] = "https://frontend.integration.test"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            var scenario = new TenantScenarioStore();
            services.AddSingleton(scenario);

            RemoveService<IUnitOfWork>(services);
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            unitOfWork.Setup(x => x.AcquireResourceLockAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            unitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            unitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            services.AddScoped(_ => unitOfWork.Object);

            RemoveService<IUserRepository>(services);
            var users = new Mock<IUserRepository>();
            users.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, CancellationToken _) => scenario.Users.GetValueOrDefault(userId));
            users.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> userIds, CancellationToken _) =>
                    userIds.Select(userId => scenario.Users.GetValueOrDefault(userId)).OfType<User>().ToArray());
            services.AddScoped(_ => users.Object);

            RemoveService<IAuthChallengeRepository>(services);
            var challenges = new Mock<IAuthChallengeRepository>();
            challenges.Setup(x => x.GetActiveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AuthChallenge?)null);
            challenges.Setup(x => x.AddAsync(It.IsAny<AuthChallenge>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddScoped(_ => challenges.Object);

            RemoveService<IUserSessionRepository>(services);
            services.AddScoped(_ => new Mock<IUserSessionRepository>().Object);

            RemoveService<IRefreshTokenRepository>(services);
            services.AddScoped(_ => new Mock<IRefreshTokenRepository>().Object);

            RemoveService<IProjectRepository>(services);
            var projects = new Mock<IProjectRepository>();
            projects.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid projectId, CancellationToken _) => scenario.Projects.GetValueOrDefault(projectId));
            services.AddScoped(_ => projects.Object);

            RemoveService<IOrganizationMemberRepository>(services);
            var organizationMembers = new Mock<IOrganizationMemberRepository>();
            organizationMembers.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid organizationId, Guid userId, CancellationToken _) =>
                    scenario.OrganizationMemberships.GetValueOrDefault((organizationId, userId)));
            organizationMembers.Setup(x => x.IsActiveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid organizationId, Guid userId, CancellationToken _) =>
                    scenario.OrganizationMemberships.TryGetValue((organizationId, userId), out var membership) &&
                    membership.Status == GodForge.Domain.Enums.MembershipStatus.Active);
            organizationMembers.Setup(x => x.GetForOrganizationsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> organizationIds, Guid userId, CancellationToken _) =>
                    organizationIds.Select(organizationId => scenario.OrganizationMemberships.GetValueOrDefault((organizationId, userId)))
                        .Where(membership => membership is { Status: GodForge.Domain.Enums.MembershipStatus.Active })
                        .Cast<GodForge.Domain.Entities.Core.OrganizationMember>()
                        .ToArray());
            services.AddScoped(_ => organizationMembers.Object);

            RemoveService<IProjectMemberRepository>(services);
            var projectMembers = new Mock<IProjectMemberRepository>();
            projectMembers.Setup(x => x.GetMembershipAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid projectId, Guid userId, CancellationToken _) =>
                    scenario.ProjectMemberships.TryGetValue((projectId, userId), out var membership) &&
                    membership.Status == GodForge.Domain.Enums.MembershipStatus.Active
                        ? membership
                        : null);
            projectMembers.Setup(x => x.GetAnyMembershipAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid projectId, Guid userId, CancellationToken _) =>
                    scenario.ProjectMemberships.GetValueOrDefault((projectId, userId)));
            projectMembers.Setup(x => x.GetMembershipsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> projectIds, Guid userId, CancellationToken _) =>
                    projectIds.Select(projectId => scenario.ProjectMemberships.GetValueOrDefault((projectId, userId)))
                        .Where(membership => membership is { Status: GodForge.Domain.Enums.MembershipStatus.Active })
                        .Cast<GodForge.Domain.Entities.Core.ProjectMember>()
                        .ToArray());
            projectMembers.Setup(x => x.GetStatisticsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<Guid> projectIds, CancellationToken _) =>
                    projectIds.Select(projectId =>
                    {
                        var active = scenario.ProjectMemberships.Values
                            .Where(membership => membership.ProjectId == projectId && membership.Status == GodForge.Domain.Enums.MembershipStatus.Active)
                            .ToArray();
                        return new GodForge.Application.Common.Models.ProjectMemberStatistics(
                            projectId,
                            active.Count(membership => membership.Role == GodForge.Domain.Enums.ProjectRole.ProjectOwner),
                            active.Length);
                    }).ToArray());
            projectMembers.Setup(x => x.GetSoleOwnerProjectIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Guid>());
            services.AddScoped(_ => projectMembers.Object);

            RemoveService<IEmailOutbox>(services);
            var emailOutbox = new Mock<IEmailOutbox>();
            emailOutbox.Setup(x => x.EnqueueAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddScoped(_ => emailOutbox.Object);

            RemoveService<IAuditWriter>(services);
            services.AddScoped(_ => new Mock<IAuditWriter>().Object);

            RemoveService<ICacheService>(services);
            services.AddSingleton(new Mock<ICacheService>().Object);

            RemoveService<IEmailService>(services);
            services.AddSingleton(new Mock<IEmailService>().Object);

            foreach (var descriptor in services
                         .Where(x => x.ServiceType == typeof(IHostedService))
                         .ToList())
            {
                services.Remove(descriptor);
            }
        });
    }

    private static void RemoveService<TService>(IServiceCollection services)
    {
        foreach (var descriptor in services.Where(x => x.ServiceType == typeof(TService)).ToList())
            services.Remove(descriptor);
    }
}
