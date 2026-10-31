using FitTrack.Application.Abstractions.Storage;

namespace FitTrack.Application.Services;

public class AvatarService(IUserRepository users, IUnitOfWork unitOfWork, IAvatarStorage storage)
{
    public const int MaxBytes = 2 * 1024 * 1024;

    public async Task<UserDto> ReplaceAsync(int userId, Stream content, long length)
    {
        if (length > MaxBytes)
        {
            throw new ValidationFailedException("file", "The image must be 2 MB or smaller.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        var bytes = buffer.ToArray();

        var extension = DetectExtension(bytes)
                        ?? throw new ValidationFailedException("file", "Upload a JPEG, PNG or WebP image.");

        var user = await users.GetForUpdateAsync(userId);
        var previous = user.AvatarFile;
        var saved = await storage.SaveAsync(bytes, extension);

        try
        {
            user.AvatarFile = saved;
            await unitOfWork.SaveChangesAsync();
        }
        catch
        {
            storage.Delete(saved);
            throw;
        }

        if (previous is not null)
        {
            storage.Delete(previous);
        }

        return user.ToDto();
    }

    public async Task<UserDto> RemoveAsync(int userId)
    {
        var user = await users.GetForUpdateAsync(userId);
        var previous = user.AvatarFile;

        user.AvatarFile = null;
        await unitOfWork.SaveChangesAsync();

        if (previous is not null)
        {
            storage.Delete(previous);
        }

        return user.ToDto();
    }

    // Goes by the file's contents, because its name and declared type are whatever
    // the client chose to send.
    private static string? DetectExtension(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith<byte>([0xFF, 0xD8, 0xFF]))
        {
            return ".jpg";
        }

        if (bytes.AsSpan().StartsWith<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return ".png";
        }

        var isWebp = bytes.Length >= 12
                     && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                     && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        return isWebp ? ".webp" : null;
    }
}
