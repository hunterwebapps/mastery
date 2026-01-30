using System.Text;
using System.Text.Json.Serialization;
using Azure.Identity;
using Mastery.Api.Middleware;
using Mastery.Api.Services;
using Mastery.Application;
using Mastery.Application.Common.Interfaces;
using Mastery.Api.HealthChecks;
using Mastery.Infrastructure;
using Mastery.Infrastructure.Data;
using Mastery.Infrastructure.Identity;
using Mastery.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
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

// Check if Service Bus messaging is enabled
var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? new ServiceBusOptions();

// When Service Bus is enabled, CAP consumers handle message processing automatically

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

// JWT Authentication + OAuth providers
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured properly.");

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings?.Secret ?? "development-secret-key-at-least-32-chars")),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };
});

// Add OAuth providers if configured
var googleClientId = builder.Configuration["Google:ClientId"]
    ?? throw new InvalidOperationException("Google OAuth ClientId is not configured.");
authBuilder.AddGoogle(options =>
{
    options.ClientId = googleClientId;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
});

var microsoftClientId = builder.Configuration["Microsoft:ClientId"]
    ?? throw new InvalidOperationException("Microsoft OAuth ClientId is not configured.");
authBuilder.AddMicrosoftAccount(options =>
{
    options.ClientId = microsoftClientId;
    options.ClientSecret = builder.Configuration["Microsoft:ClientSecret"]!;
});

// Authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireSuper", policy => policy.RequireRole(AppRoles.Super))
    .AddPolicy("RequireAdmin", policy => policy.RequireRole(AppRoles.Super, AppRoles.Admin));

// Database seeder
builder.Services.AddScoped<MasteryDbSeeder>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<ServiceBusHealthCheck>("service-bus", tags: ["messaging"]);

var app = builder.Build();

// Seed roles and super user
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<MasteryDbSeeder>();
    await seeder.SeedAsync();
}

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

// CAP Dashboard for message monitoring
app.MapGet("/cap", context =>
{
    context.Response.Redirect("/cap/dashboard");
    return Task.CompletedTask;
});

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowSpa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
