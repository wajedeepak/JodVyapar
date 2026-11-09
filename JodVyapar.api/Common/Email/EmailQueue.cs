using System.Threading.Channels;

namespace JodVyapar.Api.Common.Email;

/// <summary>
/// In-memory outbox. Requests only enqueue, so a response takes the same time whether or not an
/// email is sent (this stops anonymous endpoints leaking which emails are registered via timing).
/// MVP trade-off: emails still queued when the process stops are lost; the user can resend.
/// </summary>
public sealed class EmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(EmailMessage message) => _channel.Writer.TryWrite(message);

    internal IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Sends queued emails one at a time. A failed send is logged and dropped.</summary>
public sealed class EmailDispatcher(EmailQueue queue, IEmailSender sender, ILogger<EmailDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await sender.SendAsync(message, stoppingToken);
                logger.LogInformation("Email sent: {Subject}", message.Subject);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Email could not be sent: {Subject}", message.Subject);
            }
        }
    }
}
