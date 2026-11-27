using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

// Lógica compartida por los commands y queries de JornadasAsistencia.
internal static class OperacionesJornadasAsistencia
{
    internal const int MaximoDecididoPor = 100;

    internal static void Decidir(JornadaAsistencia jornada, string usuario)
    {
        string nombre = usuario.Length > MaximoDecididoPor ? usuario.Substring(0, MaximoDecididoPor) : usuario;
        jornada.DecididoPor = nombre;
        jornada.FechaDecision = DateTime.UtcNow;
    }

    // Reglas 19 y 20.
    internal static async Task<JornadaAsistencia> ObtenerPendienteAsync(IUnitOfWork unidad, int id)
    {
        JornadaAsistencia? jornada = await unidad.Repository<JornadaAsistencia>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (jornada is null)
        {
            throw ValidacionMarcacion.JornadaInexistente(id);
        }

        if (jornada.EstadoHorasExtra != CalculadoraJornada.HorasExtraPendiente)
        {
            throw ValidacionMarcacion.Error409($"La jornada {id} no tiene horas extra pendientes.");
        }

        return jornada;
    }
}
