using System.Text;
using FitTrack.Application.Abstractions.Storage;
using FitTrack.Application.Common;
using FitTrack.Application.Services;
using FitTrack.Domain.Entities;

namespace FitTrack.Tests;

public class AvatarServiceTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0];
    private static readonly byte[] Webp = Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 ");

    private readonly PasswordResetServiceTests.FakeUsers _users = new();
    private readonly FakeStorage _storage = new();
    private readonly AvatarService _service;
    private readonly User _user = new() { Id = 1, Name = "Sam", Email = "sam@test.com" };

    public AvatarServiceTests()
    {
        _users.Items.Add(_user);
        _service = new AvatarService(_users, new PasswordResetServiceTests.FakeUnitOfWork(), _storage);
    }

    private Task<Application.Dtos.UserDto> Upload(byte[] bytes) =>
        _service.ReplaceAsync(1, new MemoryStream(bytes), bytes.Length);

    public static TheoryData<byte[], string> Accepted => new()
    {
        { Png, ".png" },
        { Jpeg, ".jpg" },
        { Webp, ".webp" },
    };

    [Theory]
    [MemberData(nameof(Accepted))]
    public async Task Stores_images_using_the_type_found_in_their_contents(byte[] bytes, string extension)
    {
        var result = await Upload(bytes);

        Assert.EndsWith(extension, _user.AvatarFile);
        Assert.Equal($"/uploads/avatars/{_user.AvatarFile}", result.AvatarUrl);
    }

    [Theory]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>")]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("GIF89a")]
    [InlineData("")]
    public async Task Rejects_anything_else(string content)
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => Upload(Encoding.UTF8.GetBytes(content)));

        Assert.Contains("file", error.Errors.Keys);
        Assert.Empty(_storage.Files);
        Assert.Null(_user.AvatarFile);
    }

    [Fact]
    public async Task Rejects_an_image_over_the_size_limit()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _service.ReplaceAsync(1, new MemoryStream(Png), AvatarService.MaxBytes + 1));

        Assert.Contains("file", error.Errors.Keys);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Replacing_an_image_deletes_the_old_file()
    {
        await Upload(Png);
        var first = _user.AvatarFile!;

        await Upload(Jpeg);

        Assert.NotEqual(first, _user.AvatarFile);
        Assert.Equal([_user.AvatarFile!], _storage.Files);
    }

    [Fact]
    public async Task Removing_an_image_clears_it_and_deletes_the_file()
    {
        await Upload(Png);

        var result = await _service.RemoveAsync(1);

        Assert.Null(result.AvatarUrl);
        Assert.Null(_user.AvatarFile);
        Assert.Empty(_storage.Files);
    }

    private class FakeStorage : IAvatarStorage
    {
        public List<string> Files { get; } = [];

        public Task<string> SaveAsync(byte[] content, string extension)
        {
            var name = Guid.NewGuid().ToString("N") + extension;
            Files.Add(name);
            return Task.FromResult(name);
        }

        public void Delete(string fileName) => Files.Remove(fileName);
    }
}
