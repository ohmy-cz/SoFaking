using Microsoft.EntityFrameworkCore.Migrations;

namespace net.jancerveny.sofaking.DataLayer.Migrations
{
    public partial class AddingActorsAndCreators : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Actors",
                table: "Movies",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Creators",
                table: "Movies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Actors",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Creators",
                table: "Movies");
        }
    }
}
