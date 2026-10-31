using FitTrack.Application.Common;
using FitTrack.Application.Services;

namespace FitTrack.Tests;

public class StreakTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);

    [Fact]
    public void No_workouts_means_now_streak()
    {
        Assert.Equal(0, DashboardService.CurrentStreak([], Today));
    }

    [Fact]
    public void Three_days_in_a_row_ending_today_is_a_streak_of_three()
    {
        DateOnly[] days = [Today, Today.AddDays(-1), Today.AddDays(-2)];

        Assert.Equal(3, DashboardService.CurrentStreak(days, Today));
    }

    [Fact]
    public void A_streak_can_end_yesterday()
    {
        DateOnly[] days = [Today.AddDays(-1), Today.AddDays(-2)];

        Assert.Equal(2, DashboardService.CurrentStreak(days, Today));
    }

    [Fact]
    public void Current_streak_over_two_days()
    {
        DateOnly[] days = [Today, Today.AddDays(-1)];
        
        Assert.Equal(2, DashboardService.CurrentStreak(days, Today));
    }

    [Theory]
    [InlineData("Barbell Bench Press", "barbell-bench-press")]
    [InlineData("Farmer's Walk", "farmers-walk")]
    [InlineData("EZ-Bar", "ez-bar")]
    public void Slugs_match_laravel(string input, string expected)
    {
        Assert.Equal(expected, Slug.From(input));
    }
}