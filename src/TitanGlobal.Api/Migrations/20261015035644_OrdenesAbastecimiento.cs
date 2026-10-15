using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Api.Migrations
{
    /// <inheritdoc />
    public partial class OrdenesAbastecimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdenesAbastecimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProveedorId = table.Column<int>(type: "integer", nullable: false),
                    SolicitudCompraId = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaEntregaEstimada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesAbastecimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesAbastecimiento_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesAbastecimiento_SolicitudesCompra_SolicitudCompraId",
                        column: x => x.SolicitudCompraId,
                        principalTable: "SolicitudesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesOrdenesAbastecimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrdenAbastecimientoId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesOrdenesAbastecimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenesAbastecimiento_OrdenesAbastecimiento_OrdenAb~",
                        column: x => x.OrdenAbastecimientoId,
                        principalTable: "OrdenesAbastecimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenesAbastecimiento_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenesAbastecimiento_OrdenAbastecimientoId",
                table: "DetallesOrdenesAbastecimiento",
                column: "OrdenAbastecimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenesAbastecimiento_ProductoId",
                table: "DetallesOrdenesAbastecimiento",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesAbastecimiento_Numero",
                table: "OrdenesAbastecimiento",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesAbastecimiento_ProveedorId",
                table: "OrdenesAbastecimiento",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesAbastecimiento_SolicitudCompraId",
                table: "OrdenesAbastecimiento",
                column: "SolicitudCompraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesOrdenesAbastecimiento");

            migrationBuilder.DropTable(
                name: "OrdenesAbastecimiento");
        }
    }
}
