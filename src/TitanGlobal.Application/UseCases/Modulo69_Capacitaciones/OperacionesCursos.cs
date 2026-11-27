using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

// Lógica compartida por los commands y queries de Cursos.
internal static class OperacionesCursos
{
    internal static CursoListadoDto Convertir(Curso curso)
    {
        return new CursoListadoDto
        {
            Id = curso.Id,
            Codigo = curso.Codigo,
            Nombre = curso.Nombre,
            Horas = curso.Horas,
            Tipo = curso.Tipo,
            Proveedor = curso.Proveedor,
            Activo = curso.Activo
        };
    }
}
