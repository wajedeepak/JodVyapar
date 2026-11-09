using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using JodVyapar.Api.Common.Email;

namespace JodVyapar.Api.Tests;

/// <summary>Captures emails instead of sending them, so tests can read the codes.</summary>
public sealed partial class FakeEmailSender : IEmailSender
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly ConcurrentQueue<EmailMessage> _sent = new();
    private readonly ConcurrentDictionary<string, int> _read = new(StringComparer.OrdinalIgnoreCase);

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>Waits for the next not-yet-read email to this address and returns its code.</summary>
    public async Task<string> NextCodeAsync(string email) =>
        CodePattern().Match((await NextEmailAsync(email)).TextBody).Groups[1].Value;

    /// <summary>Waits for the next not-yet-read email to this address.</summary>
    public async Task<EmailMessage> NextEmailAsync(string email)
    {
        var read = _read.GetOrAdd(email, 0);
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            var message = SentTo(email).Skip(read).FirstOrDefault();
            if (message is not null)
            {
                _read[email] = read + 1;
                return message;
            }

            await Task.Delay(25);
        }

        throw new TimeoutException($"No new email to {email} within {Timeout.TotalSeconds}s.");
    }

    /// <summary>Gives the background dispatcher time to deliver, then asserts nothing new arrived.</summary>
    public async Task AssertNoNewEmailAsync(string email)
    {
        await Task.Delay(400);
        Assert.Equal(_read.GetOrAdd(email, 0), SentTo(email).Count());
    }

    private IEnumerable<EmailMessage> SentTo(string email) =>
        _sent.Where(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"Your code: (\d{6})")]
    private static partial Regex CodePattern();
}
