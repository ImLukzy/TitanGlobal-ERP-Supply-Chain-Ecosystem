using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;

public class AdministrarTurnosUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarTurnosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaTurnosDto> ListarAsync(
        string? busqueda,
        string? tipo,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        ValidacionTurno.ValidarListadoTurnos(pagina, tamanoPagina, tipoNormalizado);
        IQueryable<Turno> consulta = _unidad.Repository<Turno>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(turno => turno.Codigo.ToLower().Contains(termino)
                || turno.Nombre.ToLower().Contains(termino));
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(turno => turno.Tipo == tipoNormalizado);
        }

        if (activo is not null)
        {
            bool estaActivo = activo.Value;
            consulta = consulta.Where(turno => turno.Activo == estaActivo);
        }

        int total = await consulta.CountAsync();
        List<Turno> turnos = await consulta
            .OrderBy(turno => turno.Codigo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<TurnoListadoDto> items = new List<TurnoListadoDto>();
        foreach (Turno turno in turnos)
        {
            TurnoListadoDto dto = new TurnoListadoDto();
            ValidacionTurno.CopiarEn(turno, dto);
            items.Add(dto);
        }

        return new PaginaTurnosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ResumenTurnosDto> ResumenAsync()
    {
        DateOnly hoy = ValidacionTurno.Hoy();
        IQueryable<Turno> turnos = _unidad.Repository<Turno>().AsQueryable();
        int totalTurnos = await turnos.CountAsync();
        int activos = await turnos.CountAsync(turno => turno.Activo);
        int fijos = await turnos.CountAsync(turno => turno.Tipo == CalculadoraTurnos.TipoFijo);
        int rotativos = await turnos.CountAsync(turno => turno.Tipo == CalculadoraTurnos.TipoRotativo);
        int conTurnoHoy = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Where(asignacion => asignacion.FechaInicio <= hoy
                && (asignacion.FechaFin == null || asignacion.FechaFin >= hoy)
                && asignacion.Empleado.Estado != ValidacionTurno.EstadoEmpleadoCesado)
            .Select(asignacion => asignacion.EmpleadoId)
            .Distinct()
            .CountAsync();
        int empleadosActivos = await _unidad.Repository<Empleado>().AsQueryable()
            .CountAsync(empleado => empleado.Estado != ValidacionTurno.EstadoEmpleadoCesado);
        return new ResumenTurnosDto
        {
            TotalTurnos = totalTurnos,
            Activos = activos,
            Fijos = fijos,
            Rotativos = rotativos,
            EmpleadosConTurnoHoy = conTurnoHoy,
            EmpleadosActivosSinTurnoHoy = empleadosActivos - conTurnoHoy
        };
    }

    public async Task<TurnoDetalleDto> ObtenerAsync(int id)
    {
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, id);
        DateOnly hoy = ValidacionTurno.Hoy();
        IQueryable<AsignacionTurno> asignaciones = _unidad.Repository<AsignacionTurno>().AsQueryable()
            .Where(asignacion => asignacion.TurnoId == id);
        int totalAsignaciones = await asignaciones.CountAsync();
        int conTurnoHoy = await asignaciones
            .Where(asignacion => asignacion.FechaInicio <= hoy
                && (asignacion.FechaFin == null || asignacion.FechaFin >= hoy)
                && asignacion.Empleado.Estado != ValidacionTurno.EstadoEmpleadoCesado)
            .Select(asignacion => asignacion.EmpleadoId)
            .Distinct()
            .CountAsync();
        TurnoDetalleDto detalle = new TurnoDetalleDto
        {
            TotalAsignaciones = totalAsignaciones,
            EmpleadosConTurnoHoy = conTurnoHoy
        };
        ValidacionTurno.CopiarEn(turno, detalle);
        return detalle;
    }

    public async Task<TurnoIdDto> CrearAsync(GuardarTurnoRequestDto solicitud)
    {
        Turno turno = ValidacionTurno.ValidarTurno(solicitud);
        string codigo = turno.Codigo;
        bool existe = await _unidad.Repository<Turno>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<Turno>().AddAsync(turno);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionTurno.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        return new TurnoIdDto { Id = turno.Id };
    }

    public async Task<TurnoIdDto> ActualizarAsync(int id, GuardarTurnoRequestDto solicitud)
    {
        Turno nuevo = ValidacionTurno.ValidarTurno(solicitud);
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, id);
        bool conAsignaciones = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .AnyAsync(asignacion => asignacion.TurnoId == id);
        if (conAsignaciones)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} tiene asignaciones y no se puede modificar. Crea un turno nuevo.");
        }

        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<Turno>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != id);
        if (existe)
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        turno.Codigo = nuevo.Codigo;
        turno.Nombre = nuevo.Nombre;
        turno.Tipo = nuevo.Tipo;
        turno.HoraEntrada = nuevo.HoraEntrada;
        turno.HoraSalida = nuevo.HoraSalida;
        turno.CruzaMedianoche = nuevo.CruzaMedianoche;
        turno.ToleranciaMinutos = nuevo.ToleranciaMinutos;
        turno.DiasLaborables = nuevo.DiasLaborables;
        turno.DiasTrabajo = nuevo.DiasTrabajo;
        turno.DiasDescanso = nuevo.DiasDescanso;
        turno.FechaInicioCiclo = nuevo.FechaInicioCiclo;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionTurno.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        return new TurnoIdDto { Id = turno.Id };
    }

    public async Task<TurnoIdDto> DesactivarAsync(int id)
    {
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, id);
        if (!turno.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} ya está inactivo.");
        }

        turno.Activo = false;
        await _unidad.CompleteAsync();
        return new TurnoIdDto { Id = turno.Id };
    }

    public async Task<TurnoIdDto> ActivarAsync(int id)
    {
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, id);
        if (turno.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} ya está activo.");
        }

        turno.Activo = true;
        await _unidad.CompleteAsync();
        return new TurnoIdDto { Id = turno.Id };
    }
}
