namespace FitTrack.Application.Services;

public class MeasurementService(IBodyMeasurementRepository measurements, IUnitOfWork unitOfWork)
{
    public async Task<List<BodyMeasurementDto>> GetAllAsync(int userId)
    {
        var items = await measurements.GetForUserNewestFirstAsync(userId);
        return items.Select(m => m.ToDto()).ToList();
    }

    public async Task<BodyMeasurementDto> LogAsync(int userId, LogMeasurementRequest request)
    {
        var measurement = new BodyMeasurement
        {
            UserId = userId,
            Date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Weight = request.Weight,
            BodyFatPct = request.BodyFatPct,
            CustomMeasurements = request.CustomMeasurements?.ToEntity(),
            PhotoUrl = request.PhotoUrl,
        };

        measurements.Add(measurement);
        await unitOfWork.SaveChangesAsync();

        return measurement.ToDto();
    }
}
