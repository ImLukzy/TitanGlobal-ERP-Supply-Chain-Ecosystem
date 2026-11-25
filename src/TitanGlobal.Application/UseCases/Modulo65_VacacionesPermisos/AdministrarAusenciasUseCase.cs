using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos;

public class AdministrarAusenciasUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarAusenciasUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaAusenciasDto> ListarAsync(
        string? busqueda,
        int? empleadoId,
        string? tipo,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionAusencia.ValidarListado(pagina, tamanoPagina, desde, hasta, tipoNormalizado, estadoNormalizado);
        if (empleadoId is not null)
        {
            await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, empleadoId.Value);
        }

        IQueryable<Ausencia> consulta = _unidad.Repository<Ausencia>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(ausencia => ausencia.Numero.ToLower().Contains(termino)
                || ausencia.Empleado.Nombres.ToLower().Contains(termino)
                || ausencia.Empleado.Apellidos.ToLower().Contains(termino)
                || ausencia.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(ausencia => ausencia.EmpleadoId == idEmpleado);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(ausencia => ausencia.Tipo == tipoNormalizado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(ausencia => ausencia.Estado == estadoNormalizado);
        }

        if (desde is not null)
        {
            DateOnly inicio = desde.Value;
            consulta = consulta.Where(ausencia => ausencia.FechaFin >= inicio);
        }

        if (hasta is not null)
        {
            DateOnly fin = hasta.Value;
            consulta = consulta.Where(ausencia => ausencia.FechaInicio <= fin);
        }

        int total = await consulta.CountAsync();
        List<AusenciaListadoDto> items = await consulta
            .OrderByDescending(ausencia => ausencia.FechaInicio)
            .ThenByDescending(ausencia => ausencia.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(ausencia => new AusenciaListadoDto
            {
                Id = ausencia.Id,
                Numero = ausencia.Numero,
                EmpleadoId = ausencia.EmpleadoId,
                EmpleadoLegajo = ausencia.Empleado.Legajo,
                EmpleadoNombre = ausencia.Empleado.Apellidos + ", " + ausencia.Empleado.Nombres,
                Tipo = ausencia.Tipo,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin,
                Dias = ausencia.Dias,
                Estado = ausencia.Estado,
                FechaSolicitud = ausencia.FechaSolicitud
            })
            .ToListAsync();
        return new PaginaAusenciasDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ResumenAusenciasDto> ResumenAsync()
    {
        DateOnly hoy = ValidacionAusencia.Hoy();
        DateOnly inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        DateOnly finMes = inicioMes.AddMonths(1).AddDays(-1);
        List<FilaResumen> filas = await _unidad.Repository<Ausencia>().AsQueryable()
            .Where(ausencia => ausencia.Estado == ValidacionAusencia.EstadoPendiente
                || ausencia.Estado == ValidacionAusencia.EstadoAprobada)
            .Select(ausencia => new FilaResumen
            {
                Tipo = ausencia.Tipo,
                Estado = ausencia.Estado,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin
            })
            .ToListAsync();
        List<FilaResumen> aprobadas = filas.Where(fila => fila.Estado == ValidacionAusencia.EstadoAprobada).ToList();
        List<FilaResumen> deHoy = aprobadas.Where(fila => fila.FechaInicio <= hoy && fila.FechaFin >= hoy).ToList();
        return new ResumenAusenciasDto
        {
            Pendientes = filas.Count(fila => fila.Estado == ValidacionAusencia.EstadoPendiente),
            AusentesHoy = deHoy.Count,
            VacacionesHoy = deHoy.Count(fila => fila.Tipo == ValidacionAusencia.TipoVacaciones),
            DescansosMedicosHoy = deHoy.Count(fila => fila.Tipo == ValidacionAusencia.TipoDescansoMedico),
            AprobadasMes = aprobadas.Count(fila => fila.FechaInicio >= inicioMes && fila.FechaInicio <= finMes)
        };
    }

    public async Task<AusenciaDetalleDto> ObtenerAsync(int id)
    {
        AusenciaDetalleDto? detalle = await _unidad.Repository<Ausencia>().AsQueryable()
            .Where(ausencia => ausencia.Id == id)
            .Select(ausencia => new AusenciaDetalleDto
            {
                Id = ausencia.Id,
                Numero = ausencia.Numero,
                EmpleadoId = ausencia.EmpleadoId,
                EmpleadoLegajo = ausencia.Empleado.Legajo,
                EmpleadoNombre = ausencia.Empleado.Apellidos + ", " + ausencia.Empleado.Nombres,
                Tipo = ausencia.Tipo,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin,
                Dias = ausencia.Dias,
                Estado = ausencia.Estado,
                FechaSolicitud = ausencia.FechaSolicitud,
                Motivo = ausencia.Motivo,
                DecididoPor = ausencia.DecididoPor,
                FechaDecision = ausencia.FechaDecision,
                MotivoRechazo = ausencia.MotivoRechazo,
                FechaCancelacion = ausencia.FechaCancelacion
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionAusencia.AusenciaInexistente(id);
        }

        return detalle;
    }

    public async Task<SaldoVacacionalDto> ObtenerSaldoAsync(int empleadoId, DateOnly? fecha)
    {
        Empleado empleado = await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, empleadoId);
        DateOnly dia = fecha ?? ValidacionAusencia.Hoy();
        List<CalculadoraSaldoVacacional.PeriodoVacacional> periodos =
            await ValidacionAusencia.LeerPeriodosAsync(_unidad, empleadoId, null);
        decimal devengados = CalculadoraSaldoVacacional.Devengados(empleado.FechaIngreso, dia);
        int gozados = CalculadoraSaldoVacacional.Gozados(periodos, dia);
        int programados = CalculadoraSaldoVacacional.Programados(periodos, dia);
        return new SaldoVacacionalDto
        {
            EmpleadoId = empleado.Id,
            FechaIngreso = empleado.FechaIngreso,
            Fecha = dia,
            MesesServicio = CalculadoraSaldoVacacional.MesesCompletos(empleado.FechaIngreso, dia),
            DiasDevengados = decimal.Round(devengados, 1),
            DiasGozados = gozados,
            DiasProgramados = programados,
            Saldo = decimal.Round(CalculadoraSaldoVacacional.Saldo(devengados, gozados, programados), 1)
        };
    }

    public async Task<AusenciaCreadaDto> RegistrarAsync(RegistrarAusenciaRequestDto solicitud)
    {
        string? motivo = ValidacionAusencia.ValidarSolicitud(solicitud, out string tipo);
        int empleadoId = solicitud.EmpleadoId!.Value;
        DateOnly inicio = solicitud.FechaInicio!.Value;
        DateOnly fin = solicitud.FechaFin!.Value;
        Empleado empleado = await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionAusencia.EstadoEmpleadoActivo)
        {
            throw ValidacionAusencia.Error409($"El empleado {empleadoId} está cesado y no admite ausencias.");
        }

        if (inicio < empleado.FechaIngreso)
        {
            throw ValidacionAusencia.Error400("La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        }

        await ValidacionAusencia.ValidarSinSolapeAsync(_unidad, empleadoId, inicio, fin, null);
        int dias = ValidacionAusencia.Dias(inicio, fin);
        if (tipo == ValidacionAusencia.TipoVacaciones)
        {
            await ValidacionAusencia.ValidarSaldoAsync(_unidad, empleado, inicio, dias, null);
        }

        Ausencia ausencia = new Ausencia
        {
            Numero = ValidacionAusencia.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Tipo = tipo,
            FechaInicio = inicio,
            FechaFin = fin,
            Dias = dias,
            Motivo = motivo,
            Estado = ValidacionAusencia.EstadoPendiente,
            FechaSolicitud = ValidacionAusencia.Hoy()
        };
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await _unidad.Repository<Ausencia>().AddAsync(ausencia);
        await _unidad.CompleteAsync();
        ausencia.Numero = ValidacionAusencia.NumeroDefinitivo(ausencia.Id);
        _unidad.Repository<Ausencia>().Update(ausencia);
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new AusenciaCreadaDto { Id = ausencia.Id, Numero = ausencia.Numero };
    }

    public async Task<AusenciaIdDto> AprobarAsync(int id, string usuario)
    {
        Ausencia ausencia = await ValidacionAusencia.ObtenerAsync(_unidad, id);
        if (ausencia.Estado != ValidacionAusencia.EstadoPendiente)
        {
            throw ValidacionAusencia.NoPendiente(ausencia.Numero);
        }

        await ValidacionAusencia.ValidarSinSolapeAsync(_unidad, ausencia.EmpleadoId, ausencia.FechaInicio, ausencia.FechaFin, ausencia.Id);
        if (ausencia.Tipo == ValidacionAusencia.TipoVacaciones)
        {
            Empleado empleado = await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, ausencia.EmpleadoId);
            await ValidacionAusencia.ValidarSaldoAsync(_unidad, empleado, ausencia.FechaInicio, ausencia.Dias, ausencia.Id);
        }

        ausencia.Estado = ValidacionAusencia.EstadoAprobada;
        Decidir(ausencia, usuario);
        await _unidad.CompleteAsync();
        return new AusenciaIdDto { Id = ausencia.Id };
    }

    public async Task<AusenciaIdDto> RechazarAsync(int id, RechazarAusenciaRequestDto solicitud, string usuario)
    {
        Ausencia ausencia = await ValidacionAusencia.ObtenerAsync(_unidad, id);
        if (ausencia.Estado != ValidacionAusencia.EstadoPendiente)
        {
            throw ValidacionAusencia.NoPendiente(ausencia.Numero);
        }

        string motivo = ValidacionAusencia.ValidarMotivoRechazo(solicitud.Motivo);
        ausencia.Estado = ValidacionAusencia.EstadoRechazada;
        ausencia.MotivoRechazo = motivo;
        Decidir(ausencia, usuario);
        await _unidad.CompleteAsync();
        return new AusenciaIdDto { Id = ausencia.Id };
    }

    public async Task<AusenciaIdDto> CancelarAsync(int id)
    {
        Ausencia ausencia = await ValidacionAusencia.ObtenerAsync(_unidad, id);
        if (ausencia.Estado == ValidacionAusencia.EstadoRechazada || ausencia.Estado == ValidacionAusencia.EstadoCancelada)
        {
            throw ValidacionAusencia.Error409($"La ausencia {ausencia.Numero} ya está rechazada o cancelada.");
        }

        DateOnly hoy = ValidacionAusencia.Hoy();
        if (ausencia.FechaInicio <= hoy)
        {
            throw ValidacionAusencia.Error409($"La ausencia {ausencia.Numero} ya empezó y no se puede cancelar.");
        }

        ausencia.Estado = ValidacionAusencia.EstadoCancelada;
        ausencia.FechaCancelacion = hoy;
        await _unidad.CompleteAsync();
        return new AusenciaIdDto { Id = ausencia.Id };
    }

    private static void Decidir(Ausencia ausencia, string usuario)
    {
        string nombre = usuario.Length > ValidacionAusencia.MaximoDecididoPor
            ? usuario.Substring(0, ValidacionAusencia.MaximoDecididoPor)
            : usuario;
        ausencia.DecididoPor = nombre;
        ausencia.FechaDecision = DateTime.UtcNow;
    }

    private sealed class FilaResumen
    {
        public string Tipo { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public DateOnly FechaInicio { get; set; }

        public DateOnly FechaFin { get; set; }
    }
}
