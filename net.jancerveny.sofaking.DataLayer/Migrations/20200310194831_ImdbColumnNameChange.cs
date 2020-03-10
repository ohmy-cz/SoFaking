using Microsoft.EntityFrameworkCore.Migrations;

namespace net.jancerveny.sofaking.DataLayer.Migrations
{
    public partial class ImdbColumnNameChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImdbUrl",
                table: "Movies");

            migrationBuilder.AddColumn<string>(
                name: "ImdbId",
                table: "Movies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImdbId",
                table: "Movies");

            migrationBuilder.AddColumn<string>(
                name: "ImdbUrl",
                table: "Movies",
                type: "text",
                nullable: true);
        }
    }
}
