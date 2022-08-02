using Azure.Identity;
using Mastery.Business.Managers;
using Mastery.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Mastery.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddAzureKeyVault(
            new Uri(builder.Configuration["KeyVaultUri"]),
            new DefaultAzureCredential());

        // Configure Database
        builder.Services.AddDbContext<SqlDbContext>(options =>
        {
            var connString = builder.Configuration["MasterySqlConnString"];
            options.UseSqlServer(connString, opts => opts.MigrationsAssembly(typeof(SqlDbContext).Assembly.FullName));
        });

        // Add services to the container.
        builder.Services.AddScoped<AuthManager>();
        builder.Services.AddScoped<UserManager>();
        builder.Services.AddScoped<QuestManager>();
        builder.Services.AddScoped<EventManager>();
        builder.Services.AddScoped<EventTypeManager>();
        builder.Services.AddScoped<SkillsManager>();

        // Configure API Layer
        builder.Services.AddControllers();
        builder.Services.AddCors(opts =>
        {
            opts.AddDefaultPolicy(conf =>
            {
                conf.WithOrigins(builder.Configuration["ClientBaseUrl"])
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseHttpsRedirection();

        app.UseCors();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
