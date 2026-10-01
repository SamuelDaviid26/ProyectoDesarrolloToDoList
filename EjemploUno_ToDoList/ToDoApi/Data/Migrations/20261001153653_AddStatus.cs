using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDoApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isCompleted",
                table: "ToDoItems");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ToDoItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ToDoItems");

            migrationBuilder.AddColumn<bool>(
                name: "isCompleted",
                table: "ToDoItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
