using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Queries;

public class ListarJornadasAsistenciaQuery : IRequest<PaginaJornadasAsistenciaDto>
{
    public int? EmpleadoId { get; set; }

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public string? Estado { get; set; }

    public string? EstadoHorasExtra { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarJornadasAsistenciaQueryHandler : IRequestHandler<ListarJornadasAsistenciaQuery, PaginaJornadasAsistenciaDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarJornadasAsistenciaQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaJornadasAsistenciaDto> Handle(ListarJornadasAsistenciaQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        string horasExtraNormalizado = request.EstadoHorasExtra?.Trim() ?? string.Empty;
        ValidacionMarcacion.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        ValidacionMarcacion.ValidarFiltrosJornadas(request.Desde, request.Hasta, estadoNormalizado, horasExtraNormalizado);
        IQueryable<JornadaAsistencia> consulta = _unidad.Repository<JornadaAsistencia>().AsQueryable();
        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(jornada => jornada.EmpleadoId == idEmpleado);
        }

        if (request.Desde is not null)
        {
            DateOnly inicio = request.Desde.Value;
            consulta = consulta.Where(jornada => jornada.Fecha >= inicio);
        }

        if (request.Hasta is not null)
        {
            DateOnly fin = request.Hasta.Value;
            consulta = consulta.Where(jornada => jornada.Fecha <= fin);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(jornada => jornada.Estado == estadoNormalizado);
        }

        if (horasExtraNormalizado.Length > 0)
        {
            consulta = consulta.Where(jornada => jornada.EstadoHorasExtra == horasExtraNormalizado);
        }

        int total = await consulta.CountAsync();
        List<JornadaAsistenciaDto> items = await consulta
            .OrderByDescending(jornada => jornada.Fecha)
            .ThenBy(jornada => jornada.EmpleadoId)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(jornada => new JornadaAsistenciaDto
            {
                Id = jornada.Id,
                EmpleadoId = jornada.EmpleadoId,
                EmpleadoLegajo = jornada.Empleado.Legajo,
                EmpleadoNombre = jornada.Empleado.Apellidos + ", " + jornada.Empleado.Nombres,
                Fecha = jornada.Fecha,
                TurnoId = jornada.TurnoId,
                TurnoCodigo = jornada.Turno == null ? null : jornada.Turno.Codigo,
                HoraEntradaProgramada = jornada.HoraEntradaProgramada,
                HoraSalidaProgramada = jornada.HoraSalidaProgramada,
                ToleranciaMinutos = jornada.ToleranciaMinutos,
                Entrada = jornada.Entrada,
                Salida = jornada.Salida,
                MinutosTrabajados = jornada.MinutosTrabajados,
                MinutosTardanza = jornada.MinutosTardanza,
                MinutosExtra = jornada.MinutosExtra,
                Estado = jornada.Estado,
                EstadoHorasExtra = jornada.EstadoHorasExtra,
                DecididoPor = jornada.DecididoPor,
                FechaDecision = jornada.FechaDecision,
                MotivoRechazo = jornada.MotivoRechazo
            })
            .ToListAsync();
        return new PaginaJornadasAsistenciaDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
