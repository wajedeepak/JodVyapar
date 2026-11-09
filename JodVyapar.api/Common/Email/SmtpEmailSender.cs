using System.Diagnostics.CodeAnalysis;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace JodVyapar.Api.Common.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value.Smtp;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        var socketOptions = smtp.Security switch
        {
            SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None,
        };

        using var client = new MailKitSmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);
        if (!IsUnset(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? "", cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static bool IsUnset([NotNullWhen(false)] string? value) =>
        string.IsNullOrWhiteSpace(value) || value == AppConfigurationExtensions.SecretPlaceholder;
}
