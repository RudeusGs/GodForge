using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(Common.Behaviors.ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add Authorization service
        services.AddScoped<Common.Security.IAuthorizationService, Common.Security.AuthorizationService>();

        return services;
    }
}
