using FitTrack.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FitTrack.Tests;

public class DatabaseConnectionTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Configured_connection_string_wins_over_the_urls()
    {
        var result = DatabaseConnection.Resolve(Config(
            ("ConnectionStrings:Default", "Host=configured"),
            ("DATABASE_URL_UNPOOLED", "postgresql://u:p@unpooled/db")));

        Assert.Equal("Host=configured", result);
    }

    [Fact]
    public void Unpooled_url_is_preferred_over_the_pooled_one()
    {
        var result = DatabaseConnection.Resolve(Config(
            ("DATABASE_URL", "postgresql://u:p@pooled/db"),
            ("DATABASE_URL_UNPOOLED", "postgresql://u:p@unpooled/db")));

        Assert.Equal("unpooled", new NpgsqlConnectionStringBuilder(result).Host);
    }

    [Fact]
    public void Pooled_url_is_used_when_it_is_the_only_one()
    {
        var result = DatabaseConnection.Resolve(Config(
            ("ConnectionStrings:Default", ""),
            ("DATABASE_URL", "postgresql://u:p@pooled/db")));

        Assert.Equal("pooled", new NpgsqlConnectionStringBuilder(result).Host);
    }

    [Fact]
    public void Missing_configuration_fails_with_a_clear_message()
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(Config()));

        Assert.Contains("DATABASE_URL", error.Message);
    }

    [Fact]
    public void Url_without_a_port_gets_the_default_and_requires_ssl()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgresql://neondb_owner:secret@ep-cool-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-cool-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("neondb_owner", result.Username);
        Assert.Equal("secret", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void Url_encoded_credentials_are_decoded_and_survive_the_connection_string()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgres://us%40er:p%40ss%3Bw%3Ard%2F%3D+@host:6543/my%20db"));

        Assert.Equal(6543, result.Port);
        Assert.Equal("my db", result.Database);
        Assert.Equal("us@er", result.Username);
        Assert.Equal("p@ss;w:rd/=+", result.Password);
    }

    [Theory]
    [InlineData("Host=localhost;Password=topsecret")]
    [InlineData("mysql://user:topsecret@host/db")]
    public void Invalid_url_is_rejected_without_revealing_it(string url)
    {
        var error = Assert.Throws<InvalidOperationException>(() => DatabaseConnection.FromUrl(url));

        Assert.DoesNotContain("topsecret", error.Message);
    }
}
