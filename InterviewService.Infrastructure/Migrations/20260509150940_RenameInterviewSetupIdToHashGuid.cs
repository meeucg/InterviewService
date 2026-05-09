using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InterviewService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameInterviewSetupIdToHashGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_interviews_interview_setups_setup_id",
                table: "interviews");

            migrationBuilder.RenameColumn(
                name: "setup_id",
                table: "interviews",
                newName: "setup_hash_guid");

            migrationBuilder.RenameIndex(
                name: "ix_interviews_setup_id",
                table: "interviews",
                newName: "ix_interviews_setup_hash_guid");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "interview_setups",
                newName: "hash_guid");

            migrationBuilder.AddForeignKey(
                name: "fk_interviews_interview_setups_setup_hash_guid",
                table: "interviews",
                column: "setup_hash_guid",
                principalTable: "interview_setups",
                principalColumn: "hash_guid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_interviews_interview_setups_setup_hash_guid",
                table: "interviews");

            migrationBuilder.RenameColumn(
                name: "setup_hash_guid",
                table: "interviews",
                newName: "setup_id");

            migrationBuilder.RenameIndex(
                name: "ix_interviews_setup_hash_guid",
                table: "interviews",
                newName: "ix_interviews_setup_id");

            migrationBuilder.RenameColumn(
                name: "hash_guid",
                table: "interview_setups",
                newName: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_interviews_interview_setups_setup_id",
                table: "interviews",
                column: "setup_id",
                principalTable: "interview_setups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
