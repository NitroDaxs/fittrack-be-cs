using FitTrack.Application.Services;
using FitTrack.Domain.Entities;

namespace FitTrack.Tests;

public class SessionServiceTests
{
    private readonly PasswordResetServiceTests.FakeUsers _users = new();
    private readonly SessionService _service;
    private readonly User _user = new() { Id = 1, Name = "Sam", Email = "sam@test.com", PasswordHash = "hash" };

    public SessionServiceTests()
    {
        _users.Items.Add(_user);
        _service = new SessionService(_users);
    }

    [Fact]
    public async Task Accepts_an_active_user_with_a_current_stamp()
    {
        var user = await _service.FindValidUserAsync(1, SessionService.StampFor(_user));

        Assert.Same(_user, user);
    }

    [Fact]
    public async Task Rejects_a_suspended_user()
    {
        var stamp = SessionService.StampFor(_user);
        _user.IsActive = false;

        Assert.Null(await _service.FindValidUserAsync(1, stamp));
    }

    [Fact]
    public async Task Rejects_a_deleted_user()
    {
        var stamp = SessionService.StampFor(_user);
        _users.Items.Clear();

        Assert.Null(await _service.FindValidUserAsync(1, stamp));
    }

    [Fact]
    public async Task Rejects_a_token_issued_before_a_password_change()
    {
        var stamp = SessionService.StampFor(_user);
        _user.PasswordHash = "new-hash";

        Assert.Null(await _service.FindValidUserAsync(1, stamp));
    }

    [Fact]
    public async Task Rejects_a_token_without_a_stamp()
    {
        Assert.Null(await _service.FindValidUserAsync(1, null));
    }

    [Fact]
    public async Task Returns_the_current_role_rather_than_the_one_in_the_token()
    {
        var stamp = SessionService.StampFor(_user);
        _user.Role = UserRole.Admin;

        var user = await _service.FindValidUserAsync(1, stamp);

        Assert.Equal(UserRole.Admin, user!.Role);
    }
}
