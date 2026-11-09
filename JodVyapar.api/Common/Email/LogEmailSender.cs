namespace JodVyapar.Api.Common.Email;

/// <summary>
/// Development stand-in for SMTP: writes the plain-text email (including any code) to the log.
/// Startup refuses this provider outside Development so codes never reach real logs.
/// </summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "DEV EMAIL (not sent)\nTo: {To}\nSubject: {Subject}\n\n{Body}",
            message.To,
            message.Subject,
            message.TextBody);
        return Task.CompletedTask;
    }
}
