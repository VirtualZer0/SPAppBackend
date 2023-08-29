using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spapp_backend.Migrations
{
    /// <inheritdoc />
    public partial class sqlitelocal_migration_204 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrowdfundComment_AspNetUsers_UserId",
                table: "CrowdfundComment");

            migrationBuilder.DropForeignKey(
                name: "FK_CrowdfundComment_CrowdCompanies_CrowdCompanyId",
                table: "CrowdfundComment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrowdfundComment",
                table: "CrowdfundComment");

            migrationBuilder.RenameTable(
                name: "CrowdfundComment",
                newName: "CrowdComments");

            migrationBuilder.RenameIndex(
                name: "IX_CrowdfundComment_UserId",
                table: "CrowdComments",
                newName: "IX_CrowdComments_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CrowdfundComment_CrowdCompanyId",
                table: "CrowdComments",
                newName: "IX_CrowdComments_CrowdCompanyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrowdComments",
                table: "CrowdComments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CrowdComments_AspNetUsers_UserId",
                table: "CrowdComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrowdComments_CrowdCompanies_CrowdCompanyId",
                table: "CrowdComments",
                column: "CrowdCompanyId",
                principalTable: "CrowdCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrowdComments_AspNetUsers_UserId",
                table: "CrowdComments");

            migrationBuilder.DropForeignKey(
                name: "FK_CrowdComments_CrowdCompanies_CrowdCompanyId",
                table: "CrowdComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CrowdComments",
                table: "CrowdComments");

            migrationBuilder.RenameTable(
                name: "CrowdComments",
                newName: "CrowdfundComment");

            migrationBuilder.RenameIndex(
                name: "IX_CrowdComments_UserId",
                table: "CrowdfundComment",
                newName: "IX_CrowdfundComment_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CrowdComments_CrowdCompanyId",
                table: "CrowdfundComment",
                newName: "IX_CrowdfundComment_CrowdCompanyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrowdfundComment",
                table: "CrowdfundComment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CrowdfundComment_AspNetUsers_UserId",
                table: "CrowdfundComment",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CrowdfundComment_CrowdCompanies_CrowdCompanyId",
                table: "CrowdfundComment",
                column: "CrowdCompanyId",
                principalTable: "CrowdCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
