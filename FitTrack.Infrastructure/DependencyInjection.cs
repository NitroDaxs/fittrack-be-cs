using FitTrack.Infrastructure.Persistence;
using FitTrack.Infrastructure.Persistence.Repositories;
using FitTrack.Infrastructure.Security;
using FitTrack.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Default"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IRoutineRepository, RoutineRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IWorkoutLogRepository, WorkoutLogRepository>();
        services.AddScoped<IBodyMeasurementRepository, BodyMeasurementRepository>();
        services.AddScoped<IFavoritesRepository, FavoritesRepository>();
        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        services.AddScoped<IDashboardQueries, DashboardQueries>();
        services.AddScoped<IAdminStatsQueries, AdminStatsQueries>();
        services.AddScoped<DbSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IStorageInfo, DriveStorageInfo>();

        return services;
    }
}
