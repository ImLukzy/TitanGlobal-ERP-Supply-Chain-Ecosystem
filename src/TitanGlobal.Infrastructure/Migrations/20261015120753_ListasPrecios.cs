using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ListasPrecios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListasPrecios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SegmentoClienteId = table.Column<int>(type: "integer", nullable: true),
                    Prioridad = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasPrecios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListasPrecios_SegmentosClientes_SegmentoClienteId",
                        column: x => x.SegmentoClienteId,
                        principalTable: "SegmentosClientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemsListaPrecios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListaPreciosId = table.Column<int>(type: "integer", nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemsListaPrecios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemsListaPrecios_ListasPrecios_ListaPreciosId",
                        column: x => x.ListaPreciosId,
                        principalTable: "ListasPrecios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemsListaPrecios_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsListaPrecios_ListaPreciosId_ProductoId",
                table: "ItemsListaPrecios",
                columns: new[] { "ListaPreciosId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemsListaPrecios_ProductoId",
                table: "ItemsListaPrecios",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasPrecios_Codigo",
                table: "ListasPrecios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListasPrecios_Prioridad",
                table: "ListasPrecios",
                column: "Prioridad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListasPrecios_SegmentoClienteId",
                table: "ListasPrecios",
                column: "SegmentoClienteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemsListaPrecios");

            migrationBuilder.DropTable(
                name: "ListasPrecios");
        }
    }
}
