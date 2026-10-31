using Microsoft.Extensions.Hosting;

namespace FitTrack.Infrastructure.Storage;

public class DriveStorageInfo(IHostEnvironment env) : IStorageInfo
{
    public int GetPercentUsed()
    {
        var drive = new DriveInfo(Path.GetPathRoot(env.ContentRootPath)!);
        return drive.TotalSize == 0 ? 0 : (int)Math.Round(100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize);
    }
}
