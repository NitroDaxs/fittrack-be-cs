using Fittrack.Application.Chat;
using FitTrack.Application.Chat;
using FitTrack.Application.Chat.Tools;
using FitTrack.Application.Services;
using FitTrack.Application.Services.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<SessionService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<CalculatorService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<AvatarService>();
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

        services.AddScoped<ChatService>();
        services.AddScoped<IChatInstructions, ChatInstructions>();
        services.AddScoped<IChatTool, SearchExercisesTool>();
        services.AddScoped<IChatTool, GetExerciseTool>();
        services.AddScoped<IChatTool, SearchRoutinesTool>();
        services.AddScoped<IChatTool, GetRoutineTool>();
        services.AddScoped<IChatTool, SearchArticlesTool>();
        services.AddScoped<IChatTool, ReadArticleTool>();
        services.AddScoped<IChatTool, OneRepMaxTool>();
        services.AddScoped<IChatTool, BmiTool>();
        services.AddScoped<IChatTool, TdeeTool>();
        services.AddScoped<IChatTool, MacrosTool>();
        services.AddScoped<IChatTool, BodyFatTool>();
        services.AddScoped<IChatTool, ShowCardsTool>();

        return services;
    }
}