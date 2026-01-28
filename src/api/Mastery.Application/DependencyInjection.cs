using System.Reflection;
using FluentValidation;
using Mastery.Application.Common.Behaviors;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.CandidateGenerators;
using Mastery.Application.Features.Recommendations.Services;
using Mastery.Application.Features.Recommendations.SignalDetectors;
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
        services.AddScoped<IRecommendationPipeline, DeterministicRecommendationPipeline>();
        services.AddScoped<IUserStateAssembler, UserStateAssembler>();
        services.AddScoped<IRecommendationRanker, DefaultRecommendationRanker>();
        services.AddScoped<DeterministicRecommendationOrchestrator>();
        services.AddScoped<IRecommendationExecutor, RecommendationExecutor>();

        // Signal detectors (registered as IEnumerable<IDiagnosticSignalDetector>)
        services.AddScoped<IDiagnosticSignalDetector, HabitAdherenceDropDetector>();
        services.AddScoped<IDiagnosticSignalDetector, ProjectStuckDetector>();
        services.AddScoped<IDiagnosticSignalDetector, GoalScoreboardIncompleteDetector>();
        services.AddScoped<IDiagnosticSignalDetector, PlanRealismRiskDetector>();
        services.AddScoped<IDiagnosticSignalDetector, CheckInConsistencyDropDetector>();
        services.AddScoped<IDiagnosticSignalDetector, CapacityOverloadDetector>();
        services.AddScoped<IDiagnosticSignalDetector, EnergyTrendLowDetector>();
        services.AddScoped<IDiagnosticSignalDetector, LeadMetricDriftDetector>();
        services.AddScoped<IDiagnosticSignalDetector, NoActuatorForLeadMetricDetector>();
        services.AddScoped<IDiagnosticSignalDetector, FrictionHighDetector>();
        services.AddScoped<IDiagnosticSignalDetector, Top1FollowThroughLowDetector>();

        // Candidate generators (registered as IEnumerable<IRecommendationCandidateGenerator>)
        services.AddScoped<IRecommendationCandidateGenerator, NextBestActionGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, HabitModeSuggestionGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, ProjectStuckFixGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, Top1SuggestionGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, ExperimentRecommendationGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, GoalScoreboardSuggestionGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, HabitFromLeadMetricSuggestionGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, CheckInConsistencyNudgeGenerator>();
        services.AddScoped<IRecommendationCandidateGenerator, MetricObservationReminderGenerator>();

        return services;
    }
}
