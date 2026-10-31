namespace FitTrack.Application.Abstractions.Persistence;

public interface IBodyMeasurementRepository
{
    Task<List<BodyMeasurement>> GetForUserNewestFirstAsync(int userId);
    void Add(BodyMeasurement measurement);
}
