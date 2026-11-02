using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TitanGlobal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClientesRucUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Ruc",
                table: "Clientes",
                column: "Ruc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_Ruc",
                table: "Clientes");
        }
    }
}
