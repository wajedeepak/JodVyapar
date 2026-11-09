using Microsoft.Extensions.Options;

namespace JodVyapar.Api.Common.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>Email outbox + background sender, using the provider chosen by Email:Provider.</summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(
                o => o.Provider is EmailProviders.Smtp or EmailProviders.Log,
                "Email:Provider must be \"Smtp\" or \"Log\".")
            .Validate(
                o => o.Provider != EmailProviders.Log || environment.IsDevelopment(),
                "Email:Provider \"Log\" writes codes to the log and is only allowed in Development.")
            .Validate(o => o.Provider != EmailProviders.Smtp || IsSet(o.Smtp.Host), Missing("Email:Smtp:Host", environment))
            .Validate(o => o.Provider != EmailProviders.Smtp || o.Smtp.Port > 0, Missing("Email:Smtp:Port", environment))
            .Validate(o => o.Provider != EmailProviders.Smtp || IsSet(o.Smtp.FromAddress), Missing("Email:Smtp:FromAddress", environment))
            .ValidateOnStart();

        services.AddSingleton<IEmailSender>(sp =>
            sp.GetRequiredService<IOptions<EmailOptions>>().Value.Provider == EmailProviders.Log
                ? ActivatorUtilities.CreateInstance<LogEmailSender>(sp)
                : ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp));

        services.AddSingleton<EmailQueue>();
        services.AddHostedService<EmailDispatcher>();

        return services;
    }

    private static bool IsSet(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value != AppConfigurationExtensions.SecretPlaceholder;

    /// <summary>Says which setting is missing and where the app looked, since that is usually the real problem on a server.</summary>
    private static string Missing(string key, IHostEnvironment environment) =>
        $"{key} is missing or still the placeholder, but Email:Provider is \"Smtp\". " +
        $"Environment: '{environment.EnvironmentName}'. Settings are read from appsettings.json and appsettings.{environment.EnvironmentName}.json " +
        $"in '{environment.ContentRootPath}', then environment variables (e.g. {key.Replace(":", "__")}).";
}
