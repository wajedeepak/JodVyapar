namespace JodVyapar.Api.Common.Storage;

/// <summary>
/// Stores uploaded files by a relative path such as "products/{id}/{random}.jpg". Callers only ever
/// keep that path, so nothing outside the implementation knows where the bytes live.
///
/// Moving to Azure Blob later: add an AzureBlobFileStorage that maps the path to a blob name in one
/// container, register it instead of LocalDiskFileStorage in Program.cs, and copy the existing files
/// across keeping the same relative paths. No database or feature code changes.
/// </summary>
public interface IFileStorage
{
    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken);

    /// <summary>Null if the file doesn't exist. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken);

    /// <summary>Does nothing if the file doesn't exist.</summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken);
}

/// <summary>Bound from the "FileStorage" section.</summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute folder for uploads. Empty uses %LOCALAPPDATA%/JodVyapar/uploads. Must be outside the app's source and web root.</summary>
    public string RootPath { get; init; } = "";
}
