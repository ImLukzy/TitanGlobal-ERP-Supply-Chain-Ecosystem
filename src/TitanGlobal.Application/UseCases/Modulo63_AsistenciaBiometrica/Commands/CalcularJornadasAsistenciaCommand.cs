using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica.Commands;

public class CalcularJornadasAsistenciaCommand : IRequest<ResultadoCalculoDto>
{
    public required CalcularJornadasRequestDto Solicitud { get; set; }
}

public class CalcularJornadasAsistenciaCommandHandler : IRequestHandler<CalcularJornadasAsistenciaCommand, ResultadoCalculoDto>
{
    private readonly IUnitOfWork _unidad;

    public CalcularJornadasAsistenciaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResultadoCalculoDto> Handle(CalcularJornadasAsistenciaCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.Desde is null || request.Solicitud.Hasta is null || request.Solicitud.Desde.Value > request.Solicitud.Hasta.Value)
        {
            throw ValidacionMarcacion.Error400("Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.");
        }

        DateOnly desde = request.Solicitud.Desde.Value;
        DateOnly hasta = request.Solicitud.Hasta.Value;
        if (hasta.DayNumber - desde.DayNumber + 1 > MaximoDiasCalculo)
        {
            throw ValidacionMarcacion.Error400("El rango de cálculo no puede superar 31 días.");
        }

        if (hasta > CalculadoraJornada.HoyLocal())
        {
            throw ValidacionMarcacion.Error400("No se pueden calcular jornadas futuras.");
        }

        IQueryable<Empleado> consultaEmpleados = _unidad.Repository<Empleado>().AsQueryable();
        if (request.Solicitud.EmpleadoId is not null)
        {
            int idEmpleado = request.Solicitud.EmpleadoId.Value;
            consultaEmpleados = consultaEmpleados.Where(empleado => empleado.Id == idEmpleado);
        }

        List<Empleado> empleados = await consultaEmpleados.ToListAsync();
        if (request.Solicitud.EmpleadoId is not null && empleados.Count == 0)
        {
            throw ValidacionMarcacion.EmpleadoInexistente(request.Solicitud.EmpleadoId.Value);
        }

        List<int> empleadoIds = empleados.Select(empleado => empleado.Id).ToList();
        List<AsignacionTurno> asignaciones = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Include(asignacion => asignacion.Turno)
            .Where(asignacion => empleadoIds.Contains(asignacion.EmpleadoId)
                && asignacion.FechaInicio <= hasta
                && (asignacion.FechaFin == null || asignacion.FechaFin >= desde))
            .ToListAsync();
        DateTime inicioMarcaciones = CalculadoraJornada.LocalAUtc(desde.AddDays(-1).ToDateTime(TimeOnly.MinValue));
        DateTime finMarcaciones = CalculadoraJornada.LocalAUtc(hasta.AddDays(2).ToDateTime(TimeOnly.MinValue));
        List<Marcacion> marcaciones = await _unidad.Repository<Marcacion>().AsQueryable()
            .AsNoTracking()
            .Where(marcacion => empleadoIds.Contains(marcacion.EmpleadoId)
                && marcacion.Instante >= inicioMarcaciones
                && marcacion.Instante <= finMarcaciones)
            .ToListAsync();
        List<JornadaAsistencia> existentes = await _unidad.Repository<JornadaAsistencia>().AsQueryable()
            .Where(jornada => empleadoIds.Contains(jornada.EmpleadoId)
                && jornada.Fecha >= desde
                && jornada.Fecha <= hasta)
            .ToListAsync();
        ResultadoCalculoDto resultado = new ResultadoCalculoDto();
        foreach (Empleado empleado in empleados)
        {
            List<AsignacionTurno> asignacionesEmpleado = asignaciones.Where(asignacion => asignacion.EmpleadoId == empleado.Id).ToList();
            List<Marcacion> marcacionesEmpleado = marcaciones.Where(marcacion => marcacion.EmpleadoId == empleado.Id).ToList();
            for (DateOnly fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            {
                if (fecha < empleado.FechaIngreso)
                {
                    continue;
                }

                DateOnly dia = fecha;
                JornadaAsistencia? existente = existentes.FirstOrDefault(jornada => jornada.EmpleadoId == empleado.Id && jornada.Fecha == dia);
                if (existente is not null && EstaDecidida(existente))
                {
                    resultado.Conservadas++;
                    continue;
                }

                AsignacionTurno? asignacionDelDia = asignacionesEmpleado.FirstOrDefault(asignacion =>
                    asignacion.FechaInicio <= dia && (asignacion.FechaFin == null || asignacion.FechaFin >= dia));
                DiaProgramadoDto programado = CalculadoraTurnos.CalcularDia(asignacionDelDia?.Turno, fecha, empleado.FechaCese);
                JornadaAsistencia? calculada = CalculadoraJornada.Calcular(programado, marcacionesEmpleado);
                if (calculada is null)
                {
                    if (existente is not null)
                    {
                        _unidad.Repository<JornadaAsistencia>().Remove(existente);
                        resultado.Eliminadas++;
                    }

                    continue;
                }

                if (existente is null)
                {
                    calculada.EmpleadoId = empleado.Id;
                    await _unidad.Repository<JornadaAsistencia>().AddAsync(calculada);
                }
                else
                {
                    Copiar(calculada, existente);
                }

                resultado.Calculadas++;
            }
        }

        await _unidad.CompleteAsync();
        return resultado;
    }

    private const int MaximoDiasCalculo = 31;

    private static bool EstaDecidida(JornadaAsistencia jornada)
    {
        return jornada.EstadoHorasExtra == CalculadoraJornada.HorasExtraAprobada
            || jornada.EstadoHorasExtra == CalculadoraJornada.HorasExtraRechazada;
    }

    private static void Copiar(JornadaAsistencia origen, JornadaAsistencia destino)
    {
        destino.TurnoId = origen.TurnoId;
        destino.HoraEntradaProgramada = origen.HoraEntradaProgramada;
        destino.HoraSalidaProgramada = origen.HoraSalidaProgramada;
        destino.ToleranciaMinutos = origen.ToleranciaMinutos;
        destino.Entrada = origen.Entrada;
        destino.Salida = origen.Salida;
        destino.MinutosTrabajados = origen.MinutosTrabajados;
        destino.MinutosTardanza = origen.MinutosTardanza;
        destino.MinutosExtra = origen.MinutosExtra;
        destino.Estado = origen.Estado;
        destino.EstadoHorasExtra = origen.EstadoHorasExtra;
    }
}
