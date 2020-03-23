using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace net.jancerveny.sofaking.DataLayer.Migrations
{
    public partial class AddTranscodingStarted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TranscodingStarted",
                table: "Movies",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranscodingStarted",
                table: "Movies");
        }
    }
}
