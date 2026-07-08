using System.Text;
using ClaimsModule.API.Auth;
using ClaimsModule.API.Http;
using ClaimsModule.API.Middleware;
using ClaimsModule.Application.Claims.Commands.CreateClaim;
using ClaimsModule.Application.Claims.Mapping;
using ClaimsModule.Application.Common.Behaviors;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Services;
using ClaimsModule.Application.Reserves.Common;
using Azure.Storage.Blobs;
using ClaimsModule.Infrastructure.BackgroundJobs;
using ClaimsModule.Infrastructure.Storage;
using ClaimsModule.Persistence;
using ClaimsModule.Persistence.Services;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Bearer token - obtain one from POST /api/auth/login.",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly("ClaimsModule.Persistence")));
builder.Services.AddScoped<IClaimsDbContext>(sp => sp.GetRequiredService<ClaimsDbContext>());

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

var applicationAssembly = typeof(CreateClaimCommand).Assembly;
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(applicationAssembly);
builder.Services.AddAutoMapper(cfg => { }, typeof(ClaimsMappingProfile).Assembly);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdProvider, HttpContextCorrelationIdProvider>();
builder.Services.AddScoped<ICurrentUserService, ClaimsPrincipalCurrentUserService>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IClaimValidationService, ClaimValidationService>();
builder.Services.AddScoped<IReserveService, ReserveService>();
builder.Services.AddScoped<IClaimNumberGenerator, ClaimNumberGenerator>();
builder.Services.AddScoped<IBackgroundJobEnqueuer, HangfireBackgroundJobEnqueuer>();
builder.Services.AddScoped<PostGLReserveChangeJob>();
builder.Services.AddScoped<SlaMonitoringJob>();

// BR-D-03: Azure Blob Storage is primary; local filesystem is the dev fallback (Storage:Provider).
var storageProvider = builder.Configuration["Storage:Provider"] ?? "LocalFileSystem";
var uploadsRoot = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");

if (storageProvider == "AzureBlob")
{
    var blobConnectionString = builder.Configuration.GetConnectionString("AzureBlobStorage")
        ?? throw new InvalidOperationException("ConnectionStrings:AzureBlobStorage is required when Storage:Provider=AzureBlob.");
    builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));
    builder.Services.Configure<AzureBlobOptions>(builder.Configuration.GetSection("Storage:AzureBlob"));
    builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
}
else
{
    Directory.CreateDirectory(uploadsRoot);
    builder.Services.Configure<LocalStorageOptions>(o => o.RootPath = uploadsRoot);
    builder.Services.AddScoped<IStorageService, LocalFileSystemStorageService>();
}

// FRS 11.4: mock JWT bearer auth - three hardcoded roles, no real identity provider.
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Every endpoint requires a valid token by default; controllers/actions opt out with [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Angular dev server (and, once deployed, the Azure Static Web App origin) call this API cross-origin.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (storageProvider != "AzureBlob")
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRoot),
        RequestPath = "/uploads"
    });
}

app.UseHangfireDashboard("/hangfire");

RecurringJob.AddOrUpdate<SlaMonitoringJob>(
    "sla-monitoring", job => job.ExecuteAsync(CancellationToken.None), "*/15 * * * *");

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
    .AllowAnonymous();

app.Run();
