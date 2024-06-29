using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastorDJ.Migrations
{
    /// <inheritdoc />
    public partial class PlayedTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayedTracks",
                columns: table => new
                {
                    IdPlayedTrack = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YoutubeUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    DatePlayed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiscordServerIdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayedTracks", x => x.IdPlayedTrack);
                    table.ForeignKey(
                        name: "FK_PlayedTracks_DiscordServers_DiscordServerIdDiscordServer",
                        column: x => x.DiscordServerIdDiscordServer,
                        principalTable: "DiscordServers",
                        principalColumn: "IdDiscordServer");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayedTracks_DiscordServerIdDiscordServer",
                table: "PlayedTracks",
                column: "DiscordServerIdDiscordServer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayedTracks");
        }
    }
}
