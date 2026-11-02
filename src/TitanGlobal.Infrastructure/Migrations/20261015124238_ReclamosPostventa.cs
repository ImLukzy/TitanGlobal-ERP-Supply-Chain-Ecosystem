using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReclamosPostventa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReclamosPostventa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Prioridad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaLimite = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Resolucion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MotivoRechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclamosPostventa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReclamosPostventa_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReclamosPostventa_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReclamosPostventa_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReclamosPostventa_ClienteId",
                table: "ReclamosPostventa",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclamosPostventa_Numero",
                table: "ReclamosPostventa",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReclamosPostventa_OrdenCompraId",
                table: "ReclamosPostventa",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_ReclamosPostventa_ProductoId",
                table: "ReclamosPostventa",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReclamosPostventa");
        }
    }
}
