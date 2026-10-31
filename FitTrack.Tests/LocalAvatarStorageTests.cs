using FitTrack.Infrastructure.Storage;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace FitTrack.Tests;

public class LocalAvatarStorageTests
{
    // A new instance on a temporary disk has no uploads folder, while the database still
    // names the picture saved by an earlier instance.
    [Fact]
    public void Deleting_when_the_uploads_folder_does_not_exist_does_nothing()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storage = new LocalAvatarStorage(
            Options.Create(new StorageOptions { UploadsPath = missing }),
            new HostingEnvironment { ContentRootPath = Path.GetTempPath() });

        storage.Delete("gone.png");

        Assert.False(Directory.Exists(missing));
    }
}
