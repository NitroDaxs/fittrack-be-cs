namespace FitTrack.Infrastructure.Persistence.Repositories;

public class BodyMeasurementRepository(AppDbContext db) : IBodyMeasurementRepository
{
    public Task<List<BodyMeasurement>> GetForUserNewestFirstAsync(int userId) =>
        db.BodyMeasurements.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

    public void Add(BodyMeasurement measurement) => db.BodyMeasurements.Add(measurement);
}
