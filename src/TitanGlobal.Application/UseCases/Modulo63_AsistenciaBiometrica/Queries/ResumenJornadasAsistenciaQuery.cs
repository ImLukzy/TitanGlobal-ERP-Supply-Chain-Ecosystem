using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Queries;

public class ResumenJornadasAsistenciaQuery : IRequest<ResumenAsistenciaDto>
{
    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }
}

public class ResumenJornadasAsistenciaQueryHandler : IRequestHandler<ResumenJornadasAsistenciaQuery, ResumenAsistenciaDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenJornadasAsistenciaQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenAsistenciaDto> Handle(ResumenJornadasAsistenciaQuery request, CancellationToken cancellationToken)
    {
        if (request.Desde is null || request.Hasta is null || request.Desde.Value > request.Hasta.Value)
        {
            throw ValidacionMarcacion.Error400("Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.");
        }

        DateOnly inicio = request.Desde.Value;
        DateOnly fin = request.Hasta.Value;
        List<FilaResumen> filas = await _unidad.Repository<JornadaAsistencia>().AsQueryable()
            .Where(jornada => jornada.Fecha >= inicio && jornada.Fecha <= fin)
            .Select(jornada => new FilaResumen
            {
                Estado = jornada.Estado,
                EstadoHorasExtra = jornada.EstadoHorasExtra,
                MinutosTardanza = jornada.MinutosTardanza,
                MinutosExtra = jornada.MinutosExtra
            })
            .ToListAsync();
        return new ResumenAsistenciaDto
        {
            Jornadas = filas.Count,
            Asistencias = filas.Count(fila => fila.Estado == CalculadoraJornada.EstadoAsistencia),
            Tardanzas = filas.Count(fila => fila.Estado == CalculadoraJornada.EstadoTardanza),
            SinMarcacion = filas.Count(fila => fila.Estado == CalculadoraJornada.EstadoSinMarcacion),
            Faltas = filas.Count(fila => fila.Estado == CalculadoraJornada.EstadoFalta),
            MinutosTardanza = filas.Sum(fila => fila.MinutosTardanza),
            MinutosExtraPendientes = filas
                .Where(fila => fila.EstadoHorasExtra == CalculadoraJornada.HorasExtraPendiente)
                .Sum(fila => fila.MinutosExtra),
            MinutosExtraAprobados = filas
                .Where(fila => fila.EstadoHorasExtra == CalculadoraJornada.HorasExtraAprobada)
                .Sum(fila => fila.MinutosExtra)
        };
    }

    private sealed class FilaResumen
    {
        public string Estado { get; set; } = string.Empty;

        public string EstadoHorasExtra { get; set; } = string.Empty;

        public int MinutosTardanza { get; set; }

        public int MinutosExtra { get; set; }
    }
}
