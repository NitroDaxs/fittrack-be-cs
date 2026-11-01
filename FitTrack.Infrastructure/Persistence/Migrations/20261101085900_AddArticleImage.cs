using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "articles",
                type: "text",
                nullable: true);

            // Databases seeded before this column existed already have these articles.
            foreach (var (slug, photo) in new[]
            {
                ("how-much-protein-you-actually-need-to-build-muscle", "1532550907401-a500c9a57435"),
                ("how-many-sets-per-muscle-per-week", "1532029837206-abbe2b7620e3"),
                ("heavy-or-light-load-matters-less-than-you-think", "1517836357463-d25dfeac3438"),
                ("rest-longer-between-sets-than-you-think", "1544367567-0f2fcb009e0b"),
                ("sleep-is-a-training-variable", "1541781774459-bb2af2f05b55"),
                ("train-by-feel-reps-in-reserve-and-autoregulation", "1574680096145-d05b474e2155"),
            })
            {
                migrationBuilder.Sql(
                    $"UPDATE articles SET image_url = 'https://images.unsplash.com/photo-{photo}?auto=format&fit=crop&w=1200&q=75' WHERE slug = '{slug}';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "articles");
        }
    }
}
