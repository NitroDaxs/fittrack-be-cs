using FitTrack.Application.Services;
using FitTrack.Application.Services.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<CalculatorService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ExerciseService>();
        services.AddScoped<RoutineService>();
        services.AddScoped<ArticleService>();
        services.AddScoped<TaxonomyService>();
        services.AddScoped<FavoritesService>();
        services.AddScoped<WorkoutLogService>();
        services.AddScoped<MeasurementService>();

        services.AddScoped<AdminExerciseService>();
        services.AddScoped<AdminRoutineService>();
        services.AddScoped<AdminArticleService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<AdminStatsService>();

        return services;
    }
}
