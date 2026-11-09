namespace JodVyapar.Api.Common.Email;

public static class EmailProviders
{
    public const string Smtp = "Smtp";

    /// <summary>Writes emails (including codes) to the log. Development only.</summary>
    public const string Log = "Log";
}

public enum SmtpSecurity
{
    None,
    SslOnConnect,
    StartTls,
}

/// <summary>Bound from the "Email" section. SMTP username/password come from user-secrets / environment variables.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; init; } = EmailProviders.Smtp;
    public SmtpOptions Smtp { get; init; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    /// <summary>Leave unset (or the placeholder) for servers without authentication, e.g. smtp4dev.</summary>
    public string? Username { get; init; }
    public string? Password { get; init; }

    public string FromAddress { get; init; } = "";
    public string FromName { get; init; } = "JodVyapar";
}
