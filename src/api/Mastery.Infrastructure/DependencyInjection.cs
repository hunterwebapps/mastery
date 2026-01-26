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

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
