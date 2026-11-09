namespace JodVyapar.Api.Common.Storage;

/// <summary>A recognised image format, with the extension and content type we store it under.</summary>
public sealed record ImageFormat(string Extension, string ContentType);

/// <summary>
/// Checks an uploaded image: jpg/png/webp only, max 2 MB, and the file name, declared content type
/// and the file's actual first bytes must all agree.
/// </summary>
public static class ImageFileInspector
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private const string WrongType = "Please upload a JPG, PNG or WebP image.";

    private static readonly ImageFormat Jpeg = new("jpg", "image/jpeg");
    private static readonly ImageFormat Png = new("png", "image/png");
    private static readonly ImageFormat Webp = new("webp", "image/webp");

    private static readonly Dictionary<string, ImageFormat> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = Jpeg,
        [".jpeg"] = Jpeg,
        [".png"] = Png,
        [".webp"] = Webp,
    };

    /// <summary>The format, or null with a user-friendly error.</summary>
    public static async Task<(ImageFormat? Format, string? Error)> InspectAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return (null, "This file is empty.");
        }

        if (file.Length > MaxBytes)
        {
            return (null, "Each image must be 2 MB or smaller.");
        }

        if (!ByExtension.TryGetValue(Path.GetExtension(file.FileName), out var format)
            || !string.Equals(file.ContentType, format.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return (null, WrongType);
        }

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

        return HasSignature(format, header.AsSpan(0, read)) ? (format, null) : (null, WrongType);
    }

    private static bool HasSignature(ImageFormat format, ReadOnlySpan<byte> header)
    {
        if (format == Jpeg)
        {
            return header.StartsWith<byte>([0xFF, 0xD8, 0xFF]);
        }

        if (format == Png)
        {
            return header.StartsWith<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        }

        // RIFF....WEBP
        return header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8);
    }
}
