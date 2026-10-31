using FitTrack.Application.Abstractions.Persistence;
using FitTrack.Application.Abstractions.Security;
using FitTrack.Application.Common;
using FitTrack.Application.Dtos;
using FitTrack.Application.Services;
using FitTrack.Domain.Entities;

namespace FitTrack.Tests;

public class ProfileServiceTests
{
    private const string Password = "password123";

    private readonly PasswordResetServiceTests.FakeUsers _users = new();
    private readonly PasswordResetServiceTests.FakeTokens _resetTokens = new();
    private readonly PasswordResetServiceTests.FakeHasher _hasher = new();
    private readonly ProfileService _service;
    private readonly User _user;

    public ProfileServiceTests()
    {
        _user = new User { Id = 1, Name = "Sam", Email = "sam@test.com", PasswordHash = _hasher.Hash(Password) };
        _users.Items.Add(_user);
        _users.Items.Add(new User { Id = 2, Name = "Alex", Email = "alex@test.com", PasswordHash = "x" });
        _resetTokens.Items.Add(new PasswordResetToken { UserId = 1, TokenHash = "abc", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        _service = new ProfileService(
            _users, new NoFavorites(), _resetTokens, new PasswordResetServiceTests.FakeUnitOfWork(), _hasher, new FakeTokenIssuer());
    }

    private static void AssertField(ValidationFailedException exception, string field) =>
        Assert.Contains(field, exception.Errors.Keys);

    [Fact]
    public async Task Updates_the_name_without_a_password()
    {
        var result = await _service.UpdateAsync(1, new UpdateProfileRequest { Name = "  Samuel  " });

        Assert.Equal("Samuel", result.Name);
    }

    [Fact]
    public async Task Rejects_a_blank_name()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(
            () => _service.UpdateAsync(1, new UpdateProfileRequest { Name = "   " }));

        AssertField(error, "name");
        Assert.Equal("Sam", _user.Name);
    }

    [Fact]
    public async Task Sending_the_same_email_needs_no_password()
    {
        var result = await _service.UpdateAsync(1, new UpdateProfileRequest { Email = " SAM@test.com " });

        Assert.Equal("sam@test.com", result.Email);
        Assert.Single(_resetTokens.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-password1")]
    public async Task Changing_the_email_needs_the_current_password(string? currentPassword)
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => _service.UpdateAsync(
            1, new UpdateProfileRequest { Email = "new@test.com", CurrentPassword = currentPassword }));

        AssertField(error, "current_password");
        Assert.Equal("sam@test.com", _user.Email);
    }

    [Fact]
    public async Task Changing_the_email_cancels_reset_links_sent_to_the_old_address()
    {
        var result = await _service.UpdateAsync(
            1, new UpdateProfileRequest { Email = "New@test.com", CurrentPassword = Password });

        Assert.Equal("new@test.com", result.Email);
        Assert.Empty(_resetTokens.Items);
    }

    [Fact]
    public async Task Rejects_an_email_another_account_uses()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => _service.UpdateAsync(
            1, new UpdateProfileRequest { Email = "alex@test.com", CurrentPassword = Password }));

        AssertField(error, "email");
        Assert.Equal("sam@test.com", _user.Email);
    }

    [Fact]
    public async Task Changing_the_password_needs_the_current_one()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => _service.ChangePasswordAsync(
            1, new ChangePasswordRequest { CurrentPassword = "wrong-password1", NewPassword = "changed-pass1", NewPasswordConfirmation = "changed-pass1" }));

        AssertField(error, "current_password");
        Assert.True(_hasher.Verify(Password, _user.PasswordHash));
    }

    [Fact]
    public async Task Changing_the_password_issues_a_token_with_the_new_stamp()
    {
        var oldStamp = SessionService.StampFor(_user);

        var result = await _service.ChangePasswordAsync(
            1, new ChangePasswordRequest { CurrentPassword = Password, NewPassword = "changed-pass1", NewPasswordConfirmation = "changed-pass1" });

        Assert.True(_hasher.Verify("changed-pass1", _user.PasswordHash));
        Assert.NotEqual(oldStamp, SessionService.StampFor(_user));
        Assert.Equal(SessionService.StampFor(_user), result.AccessToken);
        Assert.Empty(_resetTokens.Items);
    }

    private class FakeTokenIssuer : ITokenService
    {
        public string CreateToken(User user) => SessionService.StampFor(user);
    }

    private class NoFavorites : IFavoritesRepository
    {
        public Task<List<Routine>> GetSavedRoutinesAsync(int userId) => throw new NotSupportedException();
        public Task<List<Exercise>> GetSavedExercisesAsync(int userId) => throw new NotSupportedException();
        public Task<bool> IsExerciseSavedAsync(int exerciseId, int userId) => throw new NotSupportedException();
        public void AddExercise(int exerciseId, int userId) => throw new NotSupportedException();
        public Task RemoveExerciseAsync(int exerciseId, int userId) => throw new NotSupportedException();
        public Task<bool> IsRoutineSavedAsync(int routineId, int userId) => throw new NotSupportedException();
        public void AddRoutine(int routineId, int userId) => throw new NotSupportedException();
        public Task RemoveRoutineAsync(int routineId, int userId) => throw new NotSupportedException();
    }
}
