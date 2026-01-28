using System.Text.Json.Serialization;
using Azure.Identity;
using Mastery.Api.Middleware;
using Mastery.Api.Services;
using Mastery.Api.Workers;
using Mastery.Application;
using Mastery.Application.Common.Interfaces;
using Mastery.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add API services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use string values for enums in JSON serialization
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Mastery API";
        document.Info.Version = "v1";
        document.Info.Description = "API for the Mastery personal development application";
        return Task.CompletedTask;
    });
});

// Background worker for proactive recommendations
builder.Services.Configure<BackgroundWorkerOptions>(
    builder.Configuration.GetSection(BackgroundWorkerOptions.SectionName));
builder.Services.AddHostedService<RecommendationBackgroundWorker>();

// Add CORS for React SPA
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpa", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Mastery API");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowSpa");
app.UseAuthorization();
app.MapControllers();

app.Run();
