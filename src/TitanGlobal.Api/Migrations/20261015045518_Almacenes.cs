using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Api.Migrations
{
    /// <inheritdoc />
    public partial class Almacenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Almacenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CapacidadM3 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Responsable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_Codigo",
                table: "Almacenes",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Almacenes");
        }
    }
}
