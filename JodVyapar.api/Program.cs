using FluentValidation;
using JodVyapar.Api.Common;
using JodVyapar.Api.Common.Activity;
using JodVyapar.Api.Common.Email;
using JodVyapar.Api.Common.Errors;
using JodVyapar.Api.Common.RateLimiting;
using JodVyapar.Api.Common.Storage;
using JodVyapar.Api.Common.Validation;
using JodVyapar.Api.Features.Catalog;
using JodVyapar.Api.Features.Identity;
using JodVyapar.Api.Features.Profiles;
using JodVyapar.Api.Features.Reference;
using JodVyapar.Api.Features.Requirements;
using JodVyapar.Api.Infrastructure.Data;
using JodVyapar.Api.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging: console + rolling file, configured in the "Serilog" section of appsettings.
builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

// Database. The schema comes from the SQL scripts in /Database (EF Core never creates tables).
// The connection string is resolved lazily so tests can override it.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseSqlServer(services.GetRequiredService<IConfiguration>().GetRequiredValue("ConnectionStrings:Default")));

// Errors are always returned as ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Controllers + FluentValidation (validators are discovered from this assembly).
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddControllers(options =>
{
    // FluentValidation owns "required" rules; avoid duplicate implicit errors for non-nullable strings.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<FluentValidationFilter>();
});

// Email outbox (SMTP, or the log in Development) and per-IP rate limits for auth endpoints.
builder.Services.AddEmail(builder.Configuration, builder.Environment);
builder.Services.AddAppRateLimiting();

// Feature services.
builder.Services.AddIdentityFeature(builder.Configuration);
builder.Services.AddScoped<ReferenceService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<ListingReferenceChecker>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<RequirementService>();
builder.Services.AddScoped<RequirementExpiryService>();
builder.Services.AddHostedService<RequirementExpiryJob>();

// Uploaded files. To move to Azure Blob, register a different IFileStorage here (see IFileStorage).
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.AddSingleton<IFileStorage, LocalDiskFileStorage>();

// CORS for the Angular dev server (origins configurable via Cors:AllowedOrigins).
const string CorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Swagger with a JWT bearer "Authorize" button.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "JodVyapar API", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /api/v1/auth/login (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = [] });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Creates the admin user only; it never creates tables.
try
{
    await AdminSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Admin seeding failed. Has the database been created with Database/01_CreateTables.sql and 02_SeedData.sql?");
}

await app.RunAsync();

// Exposed for WebApplicationFactory in the integration tests.
public partial class Program { }
