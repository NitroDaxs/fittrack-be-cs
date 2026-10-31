using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FitTrack.Infrastructure.Persistence;

public static class DatabaseConnection
{
    // Unpooled comes first: EF Core migrations need a lock that doesn't work through
    // Neon's PgBouncer pooler.
    private static readonly string[] UrlVariables = ["DATABASE_URL_UNPOOLED", "DATABASE_URL"];

    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var url = UrlVariables.Select(name => configuration[name]).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return url is null
            ? throw new InvalidOperationException(
                "No database is configured. Set ConnectionStrings:Default, DATABASE_URL_UNPOOLED or DATABASE_URL.")
            : FromUrl(url);
    }

    public static string FromUrl(string url)
    {
        // The messages leave the URL out because it contains the password.
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres"))
        {
            throw new InvalidOperationException("The database URL must be a valid postgresql:// URI.");
        }

        var credentials = uri.UserInfo.Split(':', 2);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
            SslMode = SslMode.Require,

            // Otherwise Npgsql looks for libgssapi, which the runtime image doesn't have,
            // and logs an error on the first connection.
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;
    }
}
