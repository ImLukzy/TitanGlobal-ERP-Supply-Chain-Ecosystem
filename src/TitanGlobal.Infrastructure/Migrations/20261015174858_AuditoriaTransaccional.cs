using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TitanGlobal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaTransaccional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Usuario = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Ruta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Cambios = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad_EntidadId",
                table: "RegistrosAuditoria",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Fecha",
                table: "RegistrosAuditoria",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");
        }
    }
}
