using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GodForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Add Authorization service
        services.AddScoped<Common.Security.IAuthorizationService, Common.Security.AuthorizationService>();

        return services;
    }
}
