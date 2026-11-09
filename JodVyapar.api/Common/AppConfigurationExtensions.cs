namespace JodVyapar.Api.Common;

public static class AppConfigurationExtensions
{
    /// <summary>Placeholder committed in appsettings.json for values that must come from user-secrets or environment variables.</summary>
    public const string SecretPlaceholder = "__SET_VIA_USER_SECRETS__";

    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || value == SecretPlaceholder)
        {
            throw new InvalidOperationException(
                $"Configuration value '{key}' is missing. Set it with: dotnet user-secrets set \"{key}\" \"<value>\"");
        }

        return value;
    }
}
