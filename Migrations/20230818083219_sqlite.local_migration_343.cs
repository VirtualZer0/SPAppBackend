using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace spapp_backend.Migrations
{
    /// <inheritdoc />
    public partial class sqlitelocal_migration_343 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "Folder",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Folder",
                table: "Files");
        }
    }
}
