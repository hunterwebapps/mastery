using Mastery.Application.Common.Interfaces;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Mastery.Infrastructure.Repositories;
using Mastery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Mastery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MasteryDb");

        services.AddDbContext<MasteryDbContext>(options =>
            options.UseSqlServer(connectionString, builder =>
                builder.MigrationsAssembly(typeof(MasteryDbContext).Assembly.FullName)));

        services.AddScoped<IMasteryDbContext>(provider =>
            provider.GetRequiredService<MasteryDbContext>());

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<MasteryDbContext>());

        // Register repositories
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IMetricDefinitionRepository, MetricDefinitionRepository>();
        services.AddScoped<IMetricObservationRepository, MetricObservationRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ICheckInRepository, CheckInRepository>();
        services.AddScoped<IExperimentRepository, ExperimentRepository>();
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IDiagnosticSignalRepository, DiagnosticSignalRepository>();
        services.AddScoped<IRecommendationRunHistoryRepository, RecommendationRunHistoryRepository>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // OpenAI recommendation orchestrator
        services.Configure<OpenAiOptions>(opts =>
            configuration.GetSection(OpenAiOptions.SectionName).Bind(opts));
        services.AddScoped<LlmResponseParser>();
        services.AddScoped<ILlmRecommendationOrchestrator, OpenAiLlmOrchestrator>();

        return services;
    }
}
