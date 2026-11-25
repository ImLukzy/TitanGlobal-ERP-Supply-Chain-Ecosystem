using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;

public class AdministrarJornadasAsistenciaUseCase
{
    private const int MaximoDiasCalculo = 31;

    private const int MaximoMotivoRechazo = 300;

    private const int MaximoDecididoPor = 100;

    private readonly IUnitOfWork _unidad;

    public AdministrarJornadasAsistenciaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResultadoCalculoDto> CalcularAsync(CalcularJornadasRequestDto solicitud)
    {
        if (solicitud.Desde is null || solicitud.Hasta is null || solicitud.Desde.Value > solicitud.Hasta.Value)
        {
            throw ValidacionMarcacion.Error400("Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.");
        }

        DateOnly desde = solicitud.Desde.Value;
        DateOnly hasta = solicitud.Hasta.Value;
        if (hasta.DayNumber - desde.DayNumber + 1 > MaximoDiasCalculo)
        {
            throw ValidacionMarcacion.Error400("El rango de cálculo no puede superar 31 días.");
        }

        if (hasta > CalculadoraJornada.HoyLocal())
        {
            throw ValidacionMarcacion.Error400("No se pueden calcular jornadas futuras.");
        }

        IQueryable<Empleado> consultaEmpleados = _unidad.Repository<Empleado>().AsQueryable();
        if (solicitud.EmpleadoId is not null)
        {
            int idEmpleado = solicitud.EmpleadoId.Value;
            consultaEmpleados = consultaEmpleados.Where(empleado => empleado.Id == idEmpleado);
        }

        List<Empleado> empleados = await consultaEmpleados.ToListAsync();
        if (solicitud.EmpleadoId is not null && empleados.Count == 0)
        {
            throw ValidacionMarcacion.EmpleadoInexistente(solicitud.EmpleadoId.Value);
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

    public async Task<PaginaJornadasAsistenciaDto> ListarAsync(
        int? empleadoId,
        DateOnly? desde,
        DateOnly? hasta,
        string? estado,
        string? estadoHorasExtra,
        int pagina,
        int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        string horasExtraNormalizado = estadoHorasExtra?.Trim() ?? string.Empty;
        ValidacionMarcacion.ValidarPaginacion(pagina, tamanoPagina);
        ValidacionMarcacion.ValidarFiltrosJornadas(desde, hasta, estadoNormalizado, horasExtraNormalizado);
        IQueryable<JornadaAsistencia> consulta = _unidad.Repository<JornadaAsistencia>().AsQueryable();
        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(jornada => jornada.EmpleadoId == idEmpleado);
        }

        if (desde is not null)
        {
            DateOnly inicio = desde.Value;
            consulta = consulta.Where(jornada => jornada.Fecha >= inicio);
        }

        if (hasta is not null)
        {
            DateOnly fin = hasta.Value;
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
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ResumenAsistenciaDto> ResumenAsync(DateOnly? desde, DateOnly? hasta)
    {
        if (desde is null || hasta is null || desde.Value > hasta.Value)
        {
            throw ValidacionMarcacion.Error400("Las fechas desde y hasta son obligatorias y desde no puede ser posterior a hasta.");
        }

        DateOnly inicio = desde.Value;
        DateOnly fin = hasta.Value;
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

    public async Task<JornadaAsistenciaIdDto> AprobarHorasExtraAsync(int id, string usuario)
    {
        JornadaAsistencia jornada = await ObtenerPendienteAsync(id);
        jornada.EstadoHorasExtra = CalculadoraJornada.HorasExtraAprobada;
        Decidir(jornada, usuario);
        await _unidad.CompleteAsync();
        return new JornadaAsistenciaIdDto { Id = jornada.Id };
    }

    public async Task<JornadaAsistenciaIdDto> RechazarHorasExtraAsync(int id, RechazarHorasExtraRequestDto solicitud, string usuario)
    {
        JornadaAsistencia jornada = await ObtenerPendienteAsync(id);
        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionMarcacion.Error400("El motivo del rechazo es obligatorio.");
        }

        if (motivo.Length > MaximoMotivoRechazo)
        {
            throw ValidacionMarcacion.Error400("El motivo del rechazo no puede superar 300 caracteres.");
        }

        jornada.EstadoHorasExtra = CalculadoraJornada.HorasExtraRechazada;
        jornada.MotivoRechazo = motivo;
        Decidir(jornada, usuario);
        await _unidad.CompleteAsync();
        return new JornadaAsistenciaIdDto { Id = jornada.Id };
    }

    private static bool EstaDecidida(JornadaAsistencia jornada)
    {
        return jornada.EstadoHorasExtra == CalculadoraJornada.HorasExtraAprobada
            || jornada.EstadoHorasExtra == CalculadoraJornada.HorasExtraRechazada;
    }

    private static void Decidir(JornadaAsistencia jornada, string usuario)
    {
        string nombre = usuario.Length > MaximoDecididoPor ? usuario.Substring(0, MaximoDecididoPor) : usuario;
        jornada.DecididoPor = nombre;
        jornada.FechaDecision = DateTime.UtcNow;
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

    // Reglas 19 y 20.
    private async Task<JornadaAsistencia> ObtenerPendienteAsync(int id)
    {
        JornadaAsistencia? jornada = await _unidad.Repository<JornadaAsistencia>().AsQueryable()
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

    private sealed class FilaResumen
    {
        public string Estado { get; set; } = string.Empty;

        public string EstadoHorasExtra { get; set; } = string.Empty;

        public int MinutosTardanza { get; set; }

        public int MinutosExtra { get; set; }
    }
}
