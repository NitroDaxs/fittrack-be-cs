using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoutineImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "routines",
                type: "text",
                nullable: true);

            // Databases seeded before this column existed already have these routines.
            foreach (var (slug, photo) in new[]
            {
                ("push-pull-legs-hypertrophy", "1581009146145-b5ef050c2e1e"),
                ("starting-5x5-strength", "1571019614242-c5c5dee9f50b"),
                ("upper-lower-power", "1534438327276-14e5300c3a48"),
            })
            {
                migrationBuilder.Sql(
                    $"UPDATE routines SET image_url = 'https://images.unsplash.com/photo-{photo}?auto=format&fit=crop&w=1200&q=75' WHERE slug = '{slug}';");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "routines");
        }
    }
}
