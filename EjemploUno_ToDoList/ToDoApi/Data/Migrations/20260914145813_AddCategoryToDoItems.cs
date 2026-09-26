using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToDoApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToDoItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "ToDoItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToDoItems_CategoryId",
                table: "ToDoItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ToDoItems_Categories_CategoryId",
                table: "ToDoItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ToDoItems_Categories_CategoryId",
                table: "ToDoItems");

            migrationBuilder.DropIndex(
                name: "IX_ToDoItems_CategoryId",
                table: "ToDoItems");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ToDoItems");
        }
    }
}
