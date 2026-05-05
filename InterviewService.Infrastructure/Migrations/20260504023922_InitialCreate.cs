using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "interview_setups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_setups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "interviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_interviews_interview_setups_setup_id",
                        column: x => x.setup_id,
                        principalTable: "interview_setups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interview_setups_group_name",
                table: "interview_setups",
                column: "group_name");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_setup_id",
                table: "interviews",
                column: "setup_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interviews");

            migrationBuilder.DropTable(
                name: "interview_setups");
        }
    }
}
