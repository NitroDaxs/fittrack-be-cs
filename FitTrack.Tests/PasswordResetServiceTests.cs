using FitTrack.Application.Abstractions.Email;
using FitTrack.Application.Abstractions.Persistence;
using FitTrack.Application.Abstractions.Security;
using FitTrack.Application.Common;
using FitTrack.Application.Dtos;
using FitTrack.Application.Services;
using FitTrack.Domain.Entities;

namespace FitTrack.Tests;

public class PasswordResetServiceTests
{
    private readonly FakeUsers _users = new();
    private readonly FakeTokens _tokens = new();
    private readonly FakeEmails _emails = new();
    private readonly PasswordResetService _service;
    private readonly User _user = new() { Id = 1, Name = "Sam", Email = "sam@test.com", PasswordHash = "old" };

    public PasswordResetServiceTests()
    {
        _users.Items.Add(_user);
        _service = new PasswordResetService(_users, _tokens, new FakeUnitOfWork(), new FakeHasher(), _emails);
    }

    private static ResetPasswordRequest Reset(string token) =>
        new() { Token = token, Password = "newpassword1", PasswordConfirmation = "newpassword1" };

    [Fact]
    public async Task Request_emails_a_token_and_stores_only_its_hash()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = "  SAM@test.com " });

        var sent = Assert.Single(_emails.Tokens);
        var stored = Assert.Single(_tokens.Items);
        Assert.NotEqual(sent, stored.TokenHash);
        Assert.DoesNotContain(sent, stored.TokenHash);
    }

    [Fact]
    public async Task Request_for_an_unknown_email_sends_nothing_and_does_not_throw()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = "nobody@test.com" });

        Assert.Empty(_emails.Tokens);
    }

    [Fact]
    public async Task Request_for_a_suspended_user_sends_nothing()
    {
        _user.IsActive = false;

        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });

        Assert.Empty(_emails.Tokens);
    }

    [Fact]
    public async Task A_second_request_inside_the_cooldown_sends_nothing()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });

        Assert.Single(_emails.Tokens);
    }

    [Fact]
    public async Task A_new_request_after_the_cooldown_invalidates_the_old_link()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });
        _tokens.Items[0].CreatedAt = DateTime.UtcNow.AddMinutes(-2);
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });

        await Assert.ThrowsAsync<ValidationFailedException>(() => _service.ResetAsync(Reset(_emails.Tokens[0])));
        await _service.ResetAsync(Reset(_emails.Tokens[1]));
        Assert.Equal("hashed:newpassword1", _user.PasswordHash);
    }

    [Fact]
    public async Task Reset_changes_the_password_and_the_link_only_works_once()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });

        await _service.ResetAsync(Reset(_emails.Tokens[0]));

        Assert.Equal("hashed:newpassword1", _user.PasswordHash);
        Assert.Empty(_tokens.Items);
        await Assert.ThrowsAsync<ValidationFailedException>(() => _service.ResetAsync(Reset(_emails.Tokens[0])));
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });
        _tokens.Items[0].ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        await Assert.ThrowsAsync<ValidationFailedException>(() => _service.ResetAsync(Reset(_emails.Tokens[0])));
        Assert.Equal("old", _user.PasswordHash);
    }

    [Fact]
    public async Task A_suspended_user_cannot_use_an_existing_link()
    {
        await _service.RequestAsync(new ForgotPasswordRequest { Email = _user.Email });
        _user.IsActive = false;

        await Assert.ThrowsAsync<ValidationFailedException>(() => _service.ResetAsync(Reset(_emails.Tokens[0])));
        Assert.Equal("old", _user.PasswordHash);
    }

    [Fact]
    public async Task A_made_up_token_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() => _service.ResetAsync(Reset("made-up")));
    }

    private class FakeTokens : IPasswordResetTokenRepository
    {
        public List<PasswordResetToken> Items { get; } = [];

        public Task<PasswordResetToken?> FindByHashAsync(string tokenHash) =>
            Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task<bool> WasIssuedSinceAsync(int userId, DateTime since) =>
            Task.FromResult(Items.Any(t => t.UserId == userId && t.CreatedAt >= since));

        public void Add(PasswordResetToken token)
        {
            token.CreatedAt = DateTime.UtcNow;
            Items.Add(token);
        }

        public Task<int> DeleteForUserAsync(int userId) => Task.FromResult(Items.RemoveAll(t => t.UserId == userId));
    }

    private class FakeEmails : IAccountEmailSender
    {
        public List<string> Tokens { get; } = [];

        public Task SendPasswordResetAsync(User user, string token, TimeSpan validFor)
        {
            Tokens.Add(token);
            return Task.CompletedTask;
        }
    }

    private class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == Hash(password);
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    internal class FakeUsers : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> FindAsync(int id) => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public Task<User?> FindByEmailAsync(string email) => Task.FromResult(Items.FirstOrDefault(u => u.Email == email));

        public Task<PagedResult<User>> GetPageAsync(UserFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage) =>
            throw new NotSupportedException();

        public Task<User> GetAsync(int id) => throw new NotSupportedException();
        public Task<User> GetForUpdateAsync(int id) => throw new NotSupportedException();
        public Task<bool> EmailTakenAsync(string email, int? exceptId = null) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
        public void Remove(User user) => throw new NotSupportedException();
    }
}
