using Microsoft.Extensions.Options;

namespace JodVyapar.Api.Common.Storage;

/// <summary>Keeps files in a folder on the server's disk (see <see cref="FileStorageOptions.RootPath"/>).</summary>
public sealed class LocalDiskFileStorage : IFileStorage
{
    private const int BufferSize = 81920;

    private readonly string _root;

    public LocalDiskFileStorage(IOptions<FileStorageOptions> options)
    {
        var configured = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData))
            {
                throw new InvalidOperationException("FileStorage:RootPath is not set and this account has no local app data folder. Set FileStorage:RootPath to an absolute folder.");
            }

            configured = Path.Combine(localAppData, "JodVyapar", "uploads");
        }

        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configured));
    }

    public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(path);
        Stream? stream = File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        File.Delete(Resolve(path));
        return Task.CompletedTask;
    }

    /// <summary>Full path for a storage path; refuses anything that would land outside the root.</summary>
    private string Resolve(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, path));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The storage path is outside the storage root.", nameof(path));
        }

        return fullPath;
    }
}
