using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModulo75ImpuestosTasas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Impuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Pais = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Impuestos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TasasImpuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImpuestoId = table.Column<int>(type: "integer", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasasImpuestos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TasasImpuestos_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Impuestos_Codigo",
                table: "Impuestos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TasasImpuestos_ImpuestoId_FechaInicio",
                table: "TasasImpuestos",
                columns: new[] { "ImpuestoId", "FechaInicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TasasImpuestos");

            migrationBuilder.DropTable(
                name: "Impuestos");
        }
    }
}
