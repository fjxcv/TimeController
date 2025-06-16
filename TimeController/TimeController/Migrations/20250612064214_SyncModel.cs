using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeController.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Task",
                table: "Task");

            migrationBuilder.RenameTable(
                name: "Task",
                newName: "Task");

            migrationBuilder.AddColumn<bool>(
                name: "IsCourseTask",
                table: "Task",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WeekDay",
                table: "Task",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Task",
                table: "Task",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Task",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "IsCourseTask",
                table: "Task");

            migrationBuilder.DropColumn(
                name: "WeekDay",
                table: "Task");

            migrationBuilder.RenameTable(
                name: "task",
                newName: "Task");

            migrationBuilder.AddPrimaryKey(
                name: "PK_task",
                table: "Task",
                column: "Id");
        }
    }
}
