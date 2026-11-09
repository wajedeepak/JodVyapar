using System.Text;
using JodVyapar.Api.Common;
using JodVyapar.Api.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JodVyapar.Api.Features.Identity;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>ASP.NET Core Identity (users/roles in SQL Server) + JWT bearer authentication.</summary>
    public static IServiceCollection AddIdentityFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                o => o.Key != AppConfigurationExtensions.SecretPlaceholder && Encoding.UTF8.GetByteCount(o.Key) >= 32,
                "Jwt:Key is missing or shorter than 32 characters. Set it with: dotnet user-secrets set \"Jwt:Key\" \"<random 32+ chars>\"")
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience) && o.AccessTokenMinutes > 0,
                "Jwt:Issuer, Jwt:Audience and Jwt:AccessTokenMinutes must be configured.")
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            .Validate(
                o => o.HmacKey != AppConfigurationExtensions.SecretPlaceholder && Encoding.UTF8.GetByteCount(o.HmacKey) >= 32,
                "Otp:HmacKey is missing or shorter than 32 characters. Set it with: dotnet user-secrets set \"Otp:HmacKey\" \"<random 32+ chars>\"")
            .Validate(
                o => o.CodeLifetimeMinutes > 0 && o.MaxAttempts > 0 && o.ResendCooldownSeconds >= 0 && o.MaxRequestsPerHour > 0,
                "Otp:CodeLifetimeMinutes, MaxAttempts and MaxRequestsPerHour must be positive.")
            .ValidateOnStart();

        // AddIdentityCore (not AddIdentity) so no cookie scheme is registered; the API is JWT only.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Keeps the UserLogins/UserTokens composite keys under SQL Server's 900-byte index limit
                // (must match Database/01_CreateTables.sql).
                options.Stores.MaxLengthForKeys = 128;

                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.SigningKey,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = AuthClaims.Name,
                    RoleClaimType = AuthClaims.Role,
                };
            });

        // Policies check the roles held (JWT role claims), never the client's active mode.
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.RequireBuyer, policy => policy.RequireRole(AppRoles.Buyer));
            options.AddPolicy(AuthPolicies.RequireSeller, policy => policy.RequireRole(AppRoles.Seller));
            options.AddPolicy(AuthPolicies.RequireAdmin, policy => policy.RequireRole(AppRoles.Admin));
        });

        services.AddScoped<TokenService>();
        services.AddScoped<EmailOtpService>();
        services.AddScoped<AuthService>();

        return services;
    }
}
