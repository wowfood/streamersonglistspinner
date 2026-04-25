using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServerSpinner.Functions.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpinnerStates",
                columns: table => new
                {
                    StreamerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStreamer = table.Column<string>(type: "text", nullable: false),
                    PlayedSongIdsJson = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpinnerStates", x => x.StreamerId);
                });

            migrationBuilder.CreateTable(
                name: "Streamers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TwitchUserId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    TokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Streamers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamerSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StreamerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WheelColors = table.Column<string>(type: "text", nullable: false),
                    BackgroundMode = table.Column<string>(type: "text", nullable: false),
                    BackgroundColor = table.Column<string>(type: "text", nullable: false),
                    BackgroundImage = table.Column<string>(type: "text", nullable: false),
                    DefaultStreamerName = table.Column<string>(type: "text", nullable: false),
                    HideChangeOptionWhenDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SongListFields = table.Column<string>(type: "text", nullable: false),
                    ExcludePlayedSongs = table.Column<bool>(type: "boolean", nullable: false),
                    PlayedListPosition = table.Column<string>(type: "text", nullable: false),
                    PlayHistoryPeriod = table.Column<string>(type: "text", nullable: false),
                    AutoPlay = table.Column<bool>(type: "boolean", nullable: false),
                    DebugMode = table.Column<bool>(type: "boolean", nullable: false),
                    ColorText = table.Column<string>(type: "text", nullable: false),
                    ColorStatusBackground = table.Column<string>(type: "text", nullable: false),
                    ColorPlayedListBackground = table.Column<string>(type: "text", nullable: false),
                    ColorPlayedItemBackground = table.Column<string>(type: "text", nullable: false),
                    ColorResizeHandleBackground = table.Column<string>(type: "text", nullable: false),
                    ColorResizeHandleHoverBackground = table.Column<string>(type: "text", nullable: false),
                    ColorToggleBackground = table.Column<string>(type: "text", nullable: false),
                    ColorButtonBackground = table.Column<string>(type: "text", nullable: false),
                    ColorButtonText = table.Column<string>(type: "text", nullable: false),
                    ColorPointer = table.Column<string>(type: "text", nullable: false),
                    PlayedListFontFamily = table.Column<string>(type: "text", nullable: false),
                    PlayedListFontSize = table.Column<string>(type: "text", nullable: false),
                    PlayedListMaxLines = table.Column<int>(type: "integer", nullable: false),
                    FontFamily = table.Column<string>(type: "text", nullable: false),
                    FontSize = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StreamerSonglistUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamerSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamerSettings_Streamers_StreamerId",
                        column: x => x.StreamerId,
                        principalTable: "Streamers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Streamers_TwitchUserId",
                table: "Streamers",
                column: "TwitchUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StreamerSettings_StreamerId",
                table: "StreamerSettings",
                column: "StreamerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpinnerStates");

            migrationBuilder.DropTable(
                name: "StreamerSettings");

            migrationBuilder.DropTable(
                name: "Streamers");
        }
    }
}
