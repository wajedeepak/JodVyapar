using System.Net;

namespace JodVyapar.Api.Common.Email;

/// <summary>
/// Every email the API sends, as HTML + plain text. Email clients ignore CSS variables and
/// stylesheets, so the HTML uses inline styles with the brand values from the web app's
/// _tokens.scss (primary #1556c0).
/// </summary>
public static class EmailTemplates
{
    private const string Primary = "#1556c0";
    private const string Text = "#1f2937";
    private const string Muted = "#4b5563";
    private const string Background = "#f3f5f9";
    private const string Surface = "#ffffff";
    private const string SoftPrimary = "#e8effa";

    public static EmailMessage EmailVerificationCode(string to, string fullName, string code, int validMinutes) =>
        CodeEmail(
            to,
            fullName,
            code,
            validMinutes,
            subject: "Your JodVyapar verification code",
            intro: "Use this code to verify your email address and finish setting up your JodVyapar account.");

    public static EmailMessage LoginCode(string to, string fullName, string code, int validMinutes) =>
        CodeEmail(
            to,
            fullName,
            code,
            validMinutes,
            subject: "Your JodVyapar sign-in code",
            intro: "Use this code to sign in to JodVyapar.");

    public static EmailMessage PasswordResetCode(string to, string fullName, string code, int validMinutes) =>
        CodeEmail(
            to,
            fullName,
            code,
            validMinutes,
            subject: "Your JodVyapar password reset code",
            intro: "Use this code to choose a new password for your JodVyapar account.");

    /// <summary>Sent after a password reset, so the account owner notices if it wasn't them.</summary>
    public static EmailMessage PasswordChanged(string to, string fullName)
    {
        const string subject = "Your JodVyapar password was changed";
        const string intro = "The password for your JodVyapar account was just changed. You can now sign in with your new password.";
        const string warning = "If you didn't do this, reset your password straight away with \"Forgot password?\" on the sign-in page.";

        var text = $"""
            Hi {fullName},

            {intro}

            {warning}

            JodVyapar
            """;

        var html = Layout(fullName, $"""
            <p style="margin:0 0 16px;">{intro}</p>
            <p style="margin:0;color:{Muted};font-size:14px;">{WebUtility.HtmlEncode(warning)}</p>
            """);

        return new EmailMessage(to, subject, html, text);
    }

    private static EmailMessage CodeEmail(string to, string fullName, string code, int validMinutes, string subject, string intro)
    {
        var expiry = $"This code expires in {validMinutes} minutes and can be used once.";
        const string ignore = "If you didn't request this, you can safely ignore this email. Never share this code with anyone.";

        var text = $"""
            Hi {fullName},

            {intro}

            Your code: {code}

            {expiry}

            {ignore}

            JodVyapar
            """;

        var html = Layout(fullName, $"""
            <p style="margin:0 0 20px;">{intro}</p>
            <p style="margin:0 0 20px;text-align:center;">
              <span style="display:inline-block;background:{SoftPrimary};color:{Primary};font-size:32px;font-weight:700;letter-spacing:8px;padding:12px 20px;border-radius:8px;">{code}</span>
            </p>
            <p style="margin:0 0 16px;color:{Muted};font-size:14px;">{expiry}</p>
            <p style="margin:0;color:{Muted};font-size:14px;">{ignore}</p>
            """);

        return new EmailMessage(to, subject, html, text);
    }

    /// <summary>Branded card with a greeting; <paramref name="bodyHtml"/> goes below "Hi {name},".</summary>
    private static string Layout(string fullName, string bodyHtml) =>
        $"""
        <!DOCTYPE html>
        <html lang="en">
        <body style="margin:0;padding:0;background:{Background};font-family:Inter,Segoe UI,Arial,sans-serif;color:{Text};">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{Background};padding:24px 12px;">
            <tr><td align="center">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:{Surface};border-radius:12px;overflow:hidden;">
                <tr><td style="background:{Primary};padding:20px 24px;color:#ffffff;font-size:20px;font-weight:700;">JodVyapar</td></tr>
                <tr><td style="padding:24px;font-size:16px;line-height:1.5;">
                  <p style="margin:0 0 16px;">Hi {WebUtility.HtmlEncode(fullName)},</p>
        {bodyHtml}
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
