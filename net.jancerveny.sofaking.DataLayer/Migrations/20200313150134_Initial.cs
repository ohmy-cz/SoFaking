using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace net.jancerveny.sofaking.DataLayer.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(nullable: true),
                    Added = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<DateTime>(nullable: true),
                    TorrentName = table.Column<string>(nullable: true),
                    TorrentHash = table.Column<string>(nullable: true),
                    TorrentClientTorrentId = table.Column<int>(nullable: false),
                    SizeGb = table.Column<double>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    ImdbId = table.Column<string>(nullable: true),
                    MetacriticScore = table.Column<int>(nullable: false),
                    ImdbScore = table.Column<double>(nullable: false),
                    Genres = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movies");
        }
    }
}
