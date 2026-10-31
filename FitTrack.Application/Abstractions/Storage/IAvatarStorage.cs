namespace FitTrack.Application.Abstractions.Storage;

public interface IAvatarStorage
{
    /// <summary>Stores the image under a new random name and returns that name.</summary>
    Task<string> SaveAsync(byte[] content, string extension);

    void Delete(string fileName);
}
