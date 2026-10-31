using FitTrack.Application.Abstractions.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FitTrack.Infrastructure.Storage;

public class StorageOptions
{
    public string UploadsPath { get; set; } = "uploads";

    public string UploadsRoot(IHostEnvironment env) => Path.GetFullPath(UploadsPath, env.ContentRootPath);
}

public class LocalAvatarStorage(IOptions<StorageOptions> options, IHostEnvironment env) : IAvatarStorage
{
    private string Folder => Path.Combine(options.Value.UploadsRoot(env), "avatars");

    public async Task<string> SaveAsync(byte[] content, string extension)
    {
        Directory.CreateDirectory(Folder);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(Folder, fileName), content);
        return fileName;
    }

    public void Delete(string fileName)
    {
        // The name comes from our database, but never let it point outside the folder.
        File.Delete(Path.Combine(Folder, Path.GetFileName(fileName)));
    }
}
