using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Api.Migrations
{
    /// <inheritdoc />
    public partial class DevolucionesComerciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevolucionesComerciales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "integer", nullable: false),
                    ReclamoPostventaId = table.Column<int>(type: "integer", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MotivoRechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRechazo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesComerciales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionesComerciales_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DevolucionesComerciales_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DevolucionesComerciales_ReclamosPostventa_ReclamoPostventaId",
                        column: x => x.ReclamoPostventaId,
                        principalTable: "ReclamosPostventa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesDevolucion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DevolucionComercialId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesDevolucion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesDevolucion_DevolucionesComerciales_DevolucionComerc~",
                        column: x => x.DevolucionComercialId,
                        principalTable: "DevolucionesComerciales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesDevolucion_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesDevolucion_DevolucionComercialId_ProductoId",
                table: "DetallesDevolucion",
                columns: new[] { "DevolucionComercialId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesDevolucion_ProductoId",
                table: "DetallesDevolucion",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesComerciales_ClienteId",
                table: "DevolucionesComerciales",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesComerciales_Numero",
                table: "DevolucionesComerciales",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesComerciales_OrdenCompraId",
                table: "DevolucionesComerciales",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesComerciales_ReclamoPostventaId",
                table: "DevolucionesComerciales",
                column: "ReclamoPostventaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesDevolucion");

            migrationBuilder.DropTable(
                name: "DevolucionesComerciales");
        }
    }
}
