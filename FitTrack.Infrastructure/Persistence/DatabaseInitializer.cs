namespace FitTrack.Infrastructure.Persistence;

public class DatabaseInitializer(AppDbContext db, DbSeeder seeder)
{
    public async Task RunAsync()
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            await seeder.SeedAsync();
        }
    }
}
