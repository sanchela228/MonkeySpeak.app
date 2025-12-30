using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCallSessionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ParticipantIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RoomCode = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallSessions_Users_InitiatorId",
                        column: x => x.InitiatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallSessions_InitiatorId",
                table: "CallSessions",
                column: "InitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_CallSessions_RoomCode",
                table: "CallSessions",
                column: "RoomCode");

            migrationBuilder.CreateIndex(
                name: "IX_CallSessions_Status",
                table: "CallSessions",
                column: "Status");
        }
    }
}
