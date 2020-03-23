using Microsoft.EntityFrameworkCore.Migrations;

namespace net.jancerveny.sofaking.DataLayer.Migrations
{
    public partial class AddingMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Movies",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "Movies",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EpisodeId",
                table: "Movies",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Show",
                table: "Movies",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Movies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Director",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "EpisodeId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Show",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Movies");
        }
    }
}
