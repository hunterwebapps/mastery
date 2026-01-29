using System.Reflection;
using FluentValidation;
using Mastery.Application.Common.Behaviors;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Mastery.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Recommendation pipeline services
        services.AddScoped<IRecommendationPipeline, RecommendationPipeline>();
        services.AddScoped<IUserStateAssembler, UserStateAssembler>();
        services.AddScoped<IRecommendationExecutor, RecommendationExecutor>();

        return services;
    }
}
