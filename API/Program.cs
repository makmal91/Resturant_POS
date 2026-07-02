using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using POSSystem.API.Extensions;
using POSSystem.API.Hubs;
using POSSystem.API.Services;
using POSSystem.Application.Auth.Interfaces;
using POSSystem.Application;
using POSSystem.Application.Interfaces;
using POSSystem.Infrastructure;
using POSSystem.Application.License.Interfaces;
using POSSystem.Infrastructure.Services;
using POSSystem.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetRequiredConnectionString();
builder.Services.AddPosDatabase(connectionString);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "AKHSOFT POS API", Version = "v1" });
});

// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:4173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// When hosted as IIS sub-application at /api, IIS strips the /api prefix before the request
// reaches Kestrel. Controllers use [Route("api/[controller]")], so restore the prefix.
app.Use((context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        context.Request.Path = new PathString("/api" + path);
    return next();
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<POSDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
    await DatabaseBootstrapper.InitializeAsync(db, configuration, logger, args);

    try
    {
        var backfill = scope.ServiceProvider.GetRequiredService<GlBackfillService>();
        await backfill.BackfillMissingJournalsAsync();
        logger.LogInformation("GL journal backfill completed.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "GL journal backfill skipped or partially applied.");
    }
}

var licenseService = app.Services.GetRequiredService<ILicenseService>();
await licenseService.InitializeAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionMiddleware();
app.UseRequestLoggingMiddleware();

if (builder.Configuration.GetValue("Hosting:UseHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseLicenseGateMiddleware();
app.UseLicenseEnforcementMiddleware();
app.UseBranchAccessMiddleware();
app.UsePermissionAuthorizationMiddleware();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<OrderHub>("/orderHub");

try
{
    app.Run();
}
catch (Exception ex)
{
    using var crashScope = app.Services.CreateScope();
    var crashDb = crashScope.ServiceProvider.GetRequiredService<POSDbContext>();
    var crashLogger = crashScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Application");
    await BootstrapExceptionLogger.LogAsync(crashDb, crashLogger, ex, "Application", actionName: "HostStart");
    throw;
}
