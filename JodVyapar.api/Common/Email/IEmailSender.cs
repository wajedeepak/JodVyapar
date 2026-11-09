namespace JodVyapar.Api.Common.Email;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>Delivers one email. Selected by Email:Provider ("Smtp" or, in Development only, "Log").</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
