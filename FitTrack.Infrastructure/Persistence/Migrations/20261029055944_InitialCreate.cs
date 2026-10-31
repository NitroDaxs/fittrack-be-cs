using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "muscle_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muscle_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    units = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    author_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.ForeignKey(
                        name: "fk_articles_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "body_measurements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    body_fat_pct = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    custom_measurements = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_body_measurements", x => x.id);
                    table.ForeignKey(
                        name: "fk_body_measurements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    difficulty = table.Column<string>(type: "text", nullable: false),
                    exercise_type = table.Column<string>(type: "text", nullable: false),
                    video_url = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercises", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercises_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "routines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    goal = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "text", nullable: false),
                    days_per_week = table.Column<int>(type: "integer", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routines", x => x.id);
                    table.ForeignKey(
                        name: "fk_routines_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_equipment",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_equipment", x => new { x.exercise_id, x.equipment_id });
                    table.ForeignKey(
                        name: "fk_exercise_equipment_equipment_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exercise_equipment_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_media",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_exercise_media_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_muscle_group",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    muscle_group_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_muscle_group", x => new { x.exercise_id, x.muscle_group_id });
                    table.ForeignKey(
                        name: "fk_exercise_muscle_group_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exercise_muscle_group_muscle_groups_muscle_group_id",
                        column: x => x.muscle_group_id,
                        principalTable: "muscle_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_user",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exercise_user", x => new { x.exercise_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_exercise_user_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exercise_user_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "routine_user",
                columns: table => new
                {
                    routine_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_routine_user", x => new { x.routine_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_routine_user_routines_routine_id",
                        column: x => x.routine_id,
                        principalTable: "routines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_routine_user_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    routine_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    day_order = table.Column<int>(type: "integer", nullable: true),
                    is_template = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_workouts_routines_routine_id",
                        column: x => x.routine_id,
                        principalTable: "routines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workouts_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workout_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    target_sets = table.Column<int>(type: "integer", nullable: true),
                    target_reps = table.Column<string>(type: "text", nullable: true),
                    target_weight = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    rest_seconds = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_exercises", x => x.id);
                    table.ForeignKey(
                        name: "fk_workout_exercises_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workout_exercises_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    workout_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workout_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_workout_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_workout_logs_workouts_workout_id",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "set_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workout_log_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    set_number = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    reps = table.Column<int>(type: "integer", nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_set_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_set_logs_exercises_exercise_id",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_set_logs_workout_logs_workout_log_id",
                        column: x => x.workout_log_id,
                        principalTable: "workout_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_articles_author_id",
                table: "articles",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                table: "articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_body_measurements_user_id",
                table: "body_measurements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_slug",
                table: "equipment",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exercise_equipment_equipment_id",
                table: "exercise_equipment",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_media_exercise_id",
                table: "exercise_media",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_muscle_group_muscle_group_id",
                table: "exercise_muscle_group",
                column: "muscle_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercise_user_user_id",
                table: "exercise_user",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_created_by",
                table: "exercises",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_slug",
                table: "exercises",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_muscle_groups_slug",
                table: "muscle_groups",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_routine_user_user_id",
                table: "routine_user",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_routines_created_by",
                table: "routines",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_routines_slug",
                table: "routines",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_set_logs_exercise_id",
                table: "set_logs",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_set_logs_workout_log_id",
                table: "set_logs",
                column: "workout_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workout_exercises_exercise_id",
                table: "workout_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "ix_workout_exercises_workout_id",
                table: "workout_exercises",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "ix_workout_logs_user_id",
                table: "workout_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_workout_logs_workout_id",
                table: "workout_logs",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "ix_workouts_created_by",
                table: "workouts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_workouts_routine_id",
                table: "workouts",
                column: "routine_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "body_measurements");

            migrationBuilder.DropTable(
                name: "exercise_equipment");

            migrationBuilder.DropTable(
                name: "exercise_media");

            migrationBuilder.DropTable(
                name: "exercise_muscle_group");

            migrationBuilder.DropTable(
                name: "exercise_user");

            migrationBuilder.DropTable(
                name: "routine_user");

            migrationBuilder.DropTable(
                name: "set_logs");

            migrationBuilder.DropTable(
                name: "workout_exercises");

            migrationBuilder.DropTable(
                name: "equipment");

            migrationBuilder.DropTable(
                name: "muscle_groups");

            migrationBuilder.DropTable(
                name: "workout_logs");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "workouts");

            migrationBuilder.DropTable(
                name: "routines");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
