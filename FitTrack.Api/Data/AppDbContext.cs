namespace FitTrack.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseMuscleGroup> ExerciseMuscleGroups => Set<ExerciseMuscleGroup>();
    public DbSet<ExerciseEquipment> ExerciseEquipment => Set<ExerciseEquipment>();
    public DbSet<ExerciseMedia> ExerciseMedia => Set<ExerciseMedia>();
    public DbSet<Routine> Routines => Set<Routine>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();
    public DbSet<SetLog> SetLogs => Set<SetLog>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<SavedExercise> SavedExercises => Set<SavedExercise>();
    public DbSet<SavedRoutine> SavedRoutines => Set<SavedRoutine>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<UserRole>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<Units>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<Difficulty>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<ExerciseCategory>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<RoutineGoal>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<ArticleCategory>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<ArticleStatus>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<MuscleRole>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();
        builder.Properties<MediaType>().HaveConversion<SnakeCaseEnumConverter<UserRole>>();

        builder.Properties<decimal>().HavePrecision(8, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(user => { user.HasIndex(u => u.Email).IsUnique(); });

        builder.Entity<MuscleGroup>().HasIndex(m => m.Slug).IsUnique();
        builder.Entity<Equipment>().HasIndex(e => e.Slug).IsUnique();

        builder.Entity<Exercise>(exercise =>
        {
            exercise.HasIndex(e => e.Slug).IsUnique();
            exercise.HasOne(e => e.Creator).WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExerciseMuscleGroup>(link =>
        {
            link.ToTable("exercise_muscle_group");
            link.HasKey(l => new { l.ExerciseId, l.MuscleGroupId });
        });

        builder.Entity<ExerciseEquipment>(link =>
        {
            link.ToTable("exercise_equipment");
            link.HasKey(l => new { l.ExerciseId, l.EquipmentId });
        });

        builder.Entity<Routine>(routine =>
        {
            routine.HasIndex(r => r.Slug).IsUnique();
            routine.HasOne(r => r.Creator).WithMany().HasForeignKey(r => r.CreatedBy);
        });

        builder.Entity<Workout>(workout =>
        {
            workout.HasOne(w => w.Routine).WithMany(r => r.Workouts)
                .HasForeignKey(w => w.RoutineId)
                .OnDelete(DeleteBehavior.Cascade);
            workout.HasOne(w => w.Creator).WithMany().HasForeignKey(w => w.CreatedBy);
        });

        builder.Entity<WorkoutLog>(log =>
        {
            log.HasOne(l => l.Workout).WithMany()
                .HasForeignKey(l => l.WorkoutId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<BodyMeasurement>(measurement =>
        {
            measurement.Property(m => m.Weight).HasPrecision(5, 2);
            measurement.Property(m => m.BodyFatPct).HasPrecision(4, 1);
            measurement.OwnsOne(m => m.CustomMeasurements, json => json.ToJson());
        });

        builder.Entity<Article>(article => { article.HasIndex(a => a.Slug).IsUnique(); });

        builder.Entity<SavedExercise>(saved =>
        {
            saved.ToTable("exercise_user");
            saved.HasKey(s => new { s.ExerciseId, s.UserId });
        });

        builder.Entity<SavedRoutine>(saved =>
        {
            saved.ToTable("routine_user");
            saved.HasKey(s => new { s.RoutineId, s.UserId });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Timestamped>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}