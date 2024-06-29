using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastorDJ.Migrations
{
    /// <inheritdoc />
    public partial class Playlists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordServers",
                columns: table => new
                {
                    IdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscordId = table.Column<long>(type: "bigint", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServers", x => x.IdDiscordServer);
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                columns: table => new
                {
                    IdPlaylist = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiscordServerIdDiscordServer = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.IdPlaylist);
                    table.ForeignKey(
                        name: "FK_Playlists_DiscordServers_DiscordServerIdDiscordServer",
                        column: x => x.DiscordServerIdDiscordServer,
                        principalTable: "DiscordServers",
                        principalColumn: "IdDiscordServer");
                });

            migrationBuilder.CreateTable(
                name: "PlaylistItems",
                columns: table => new
                {
                    IdPlaylistItem = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdPlaylist = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YoutubeUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlaylistIdPlaylist = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistItems", x => x.IdPlaylistItem);
                    table.ForeignKey(
                        name: "FK_PlaylistItems_Playlists_PlaylistIdPlaylist",
                        column: x => x.PlaylistIdPlaylist,
                        principalTable: "Playlists",
                        principalColumn: "IdPlaylist");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_PlaylistIdPlaylist",
                table: "PlaylistItems",
                column: "PlaylistIdPlaylist");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_DiscordServerIdDiscordServer",
                table: "Playlists",
                column: "DiscordServerIdDiscordServer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaylistItems");

            migrationBuilder.DropTable(
                name: "Playlists");

            migrationBuilder.DropTable(
                name: "DiscordServers");
        }
    }
}
