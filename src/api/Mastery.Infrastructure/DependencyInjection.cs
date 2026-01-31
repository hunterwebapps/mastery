using Azure.Identity;
using Mastery.Application.Common.Interfaces;
using Mastery.Application.Features.Recommendations.Services;
using Mastery.Domain.Entities;
using Mastery.Domain.Entities.CheckIn;
using Mastery.Domain.Entities.Experiment;
using Mastery.Domain.Entities.Goal;
using Mastery.Domain.Entities.Habit;
using Mastery.Domain.Entities.Metrics;
using Mastery.Domain.Entities.Project;
using Mastery.Domain.Entities.UserProfile;
using Mastery.Domain.Interfaces;
using Mastery.Infrastructure.Data;
using Mastery.Infrastructure.Embeddings;
using Mastery.Infrastructure.Embeddings.Strategies;
using Mastery.Infrastructure.Identity;
using Mastery.Infrastructure.Identity.Services;
using Mastery.Infrastructure.Messaging;
using Mastery.Infrastructure.Repositories;
using Mastery.Infrastructure.Services;
using Mastery.Infrastructure.Services.Rules;
using Mastery.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Task = Mastery.Domain.Entities.Task.Task;


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

        // Register Domain Event Dispatcher for transactional event handling
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

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
        services.AddScoped<IRecommendationRunHistoryRepository, RecommendationRunHistoryRepository>();

        // Signal queue services
        services.AddScoped<ISignalEntryRepository, SignalEntryRepository>();
        services.AddScoped<ISignalProcessingHistoryRepository, SignalProcessingHistoryRepository>();
        services.AddSingleton<ISignalClassifier, SignalClassifier>();
        services.AddScoped<IUserScheduleResolver, UserScheduleResolver>();

        // Signal coverage validation at startup
        services.AddHostedService<SignalCoverageValidator>();

        // Tier 0 deterministic rules engine
        services.AddScoped<IDeterministicRule, TaskCapacityOverloadRule>();
        services.AddScoped<IDeterministicRule, DeadlineProximityRule>();
        services.AddScoped<IDeterministicRule, HabitStreakBreakDetectionRule>();
        services.AddScoped<IDeterministicRule, HabitAdherenceThresholdRule>();
        services.AddScoped<IDeterministicRule, TaskEnergyMismatchRule>();
        services.AddScoped<IDeterministicRule, ExperimentStaleRule>();
        services.AddScoped<IDeterministicRule, CheckInNoTop1SelectedRule>();
        services.AddScoped<IDeterministicRule, CheckInMissingRule>();
        services.AddScoped<IDeterministicRule, GoalScoreboardIncompleteRule>();
        services.AddScoped<IDeterministicRule, GoalProgressAtRiskRule>();
        services.AddScoped<IDeterministicRule, ProjectStuckRule>();
        services.AddScoped<IDeterministicRule, MetricObservationOverdueRule>();
        services.AddScoped<IDeterministicRule, RecurringTaskStalenessRule>();
        services.AddScoped<IDeterministicRule, TaskOverdueRule>();
        services.AddScoped<IDeterministicRulesEngine, DeterministicRulesEngine>();

        // Tier 1 quick assessment services
        services.AddScoped<IStateDeltaCalculator, StateDeltaCalculator>();
        services.AddScoped<IQuickAssessmentService, QuickAssessmentService>();

        // Tiered assessment orchestrator (Tier 0 → Tier 1 → Tier 2)
        services.AddScoped<ITieredAssessmentEngine, TieredAssessmentEngine>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        // OpenAI configuration
        services.Configure<OpenAiOptions>(opts =>
            configuration.GetSection(OpenAiOptions.SectionName).Bind(opts));

        // OpenAI recommendation orchestrator
        services.AddScoped<LlmResponseParser>();
        services.AddScoped<IRecommendationOrchestrator, OpenAiLlmOrchestrator>();

        // Simple action executor for server-side recommendation actions (ExecuteToday, Defer)
        services.AddScoped<ISimpleActionExecutor, SimpleActionExecutor>();

        // Tool call handler for LLM executor
        services.AddScoped<IToolCallHandler, OpenAiToolCallHandler>();

        // LLM executor for recommendation actions via OpenAI tool calling
        services.AddScoped<ILlmExecutor, OpenAiLlmExecutor>();

        // Vector store / embedding services
        AddVectorStoreServices(services, configuration);

        // CAP + Service Bus messaging
        services.AddMessaging(configuration);

        // Application Insights telemetry
        services.AddTelemetry(configuration);

        // Identity configuration
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password requirements (balanced security/usability)
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            // Lockout policy
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User requirements
            options.User.RequireUniqueEmail = true;

            // Minimal friction - no email confirmation required
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<MasteryDbContext>()
        .AddDefaultTokenProviders();

        // JWT configuration
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }

    private static void AddVectorStoreServices(IServiceCollection services, IConfiguration configuration)
    {
        // Cosmos DB configuration
        services.Configure<CosmosDbOptions>(opts =>
            configuration.GetSection(CosmosDbOptions.SectionName).Bind(opts));

        var cosmosOptions = configuration.GetSection(CosmosDbOptions.SectionName).Get<CosmosDbOptions>()
            ?? throw new InvalidOperationException("CosmosDbOptions configuration section is missing");

        // Register Cosmos client as singleton
        services.AddSingleton(sp =>
        {
            var options = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                },
                ConnectionMode = ConnectionMode.Direct,
                MaxRetryAttemptsOnRateLimitedRequests = 5,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
            };

            return new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.Key, options);
        });

        services.AddScoped<IEntityResolver, EntityResolver>();

        // Register vector store
        services.AddScoped<IVectorStore, CosmosVectorStore>();

        // Register embedding service
        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();

        // Register embedding text strategy factory
        services.AddScoped<IEmbeddingTextStrategyFactory, EmbeddingTextStrategyFactory>();

        // Register individual embedding text strategies
        services.AddScoped<IEmbeddingTextStrategy<Goal>, GoalEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<Habit>, HabitEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<Task>, TaskEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<Project>, ProjectEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<CheckIn>, CheckInEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<Experiment>, ExperimentEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<UserProfile>, UserProfileEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<Season>, SeasonEmbeddingTextStrategy>();
        services.AddScoped<IEmbeddingTextStrategy<MetricDefinition>, MetricDefinitionEmbeddingTextStrategy>();
    }
}
