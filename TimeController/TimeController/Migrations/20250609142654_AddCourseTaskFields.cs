using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeController.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseTaskFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 移除尝试删除不存在字段的操作
            // migrationBuilder.DropColumn(
            //     name: "IsCourse",
            //     table: "Tasks");

            migrationBuilder.AddColumn<bool>(
                name: "IsCourseTask",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WeekDay",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCourseTask",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "WeekDay",
                table: "Tasks");

            // 移除尝试添加不需要的字段的操作
            // migrationBuilder.AddColumn<bool>(
            //     name: "IsCourse",
            //     table: "Tasks",
            //     type: "INTEGER",
            //     nullable: false,
            //     defaultValue: false);
        }
    }
}
