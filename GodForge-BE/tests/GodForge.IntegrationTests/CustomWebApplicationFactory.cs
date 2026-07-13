using System.Data.Common;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace GodForge.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's GodForgeDbContext registration and options
            var unitOfWorkDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
            if (unitOfWorkDescriptor != null) services.Remove(unitOfWorkDescriptor);
            var uowMock = new Mock<IUnitOfWork>();
            services.AddScoped(sp => uowMock.Object);

            var userRepoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserRepository));
            if (userRepoDescriptor != null) services.Remove(userRepoDescriptor);
            var userMock = new Mock<IUserRepository>();
            services.AddScoped(sp => userMock.Object);

            var projectRepoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProjectRepository));
            if (projectRepoDescriptor != null) services.Remove(projectRepoDescriptor);
            var projectMock = new Mock<IProjectRepository>();
            services.AddScoped(sp => projectMock.Object);

            // Mock Redis / Cache Service
            var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
            if (cacheDescriptor != null) services.Remove(cacheDescriptor);
            var cacheMock = new Mock<ICacheService>();
            services.AddSingleton(cacheMock.Object);

            // Mock Email Service
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);
            var emailMock = new Mock<IEmailService>();
            services.AddSingleton(emailMock.Object);
        });
    }
}
