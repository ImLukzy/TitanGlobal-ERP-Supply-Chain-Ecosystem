using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

public class AdministrarIncidentesSstUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarIncidentesSstUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaIncidentesSstDto> ListarAsync(
        string? busqueda,
        string? tipo,
        string? severidad,
        string? estado,
        DateOnly? desde,
        DateOnly? hasta,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        string severidadNormalizada = severidad?.Trim() ?? string.Empty;
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionSst.ValidarPagina(pagina, tamanoPagina);
        ValidacionSst.ValidarFiltrosIncidente(tipoNormalizado, severidadNormalizada, estadoNormalizado, desde, hasta);
        IQueryable<IncidenteSst> consulta = _unidad.Repository<IncidenteSst>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(incidente => incidente.Numero.ToLower().Contains(termino)
                || incidente.Descripcion.ToLower().Contains(termino)
                || (incidente.Lugar != null && incidente.Lugar.ToLower().Contains(termino)));
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(incidente => incidente.Tipo == tipoNormalizado);
        }

        if (severidadNormalizada.Length > 0)
        {
            consulta = consulta.Where(incidente => incidente.Severidad == severidadNormalizada);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(incidente => incidente.Estado == estadoNormalizado);
        }

        if (desde is not null)
        {
            DateOnly inicio = desde.Value;
            consulta = consulta.Where(incidente => incidente.Fecha >= inicio);
        }

        if (hasta is not null)
        {
            DateOnly fin = hasta.Value;
            consulta = consulta.Where(incidente => incidente.Fecha <= fin);
        }

        int total = await consulta.CountAsync();
        List<IncidenteSst> incidentes = await consulta
            .Include(incidente => incidente.Empleado)
            .Include(incidente => incidente.AccionesCorrectivasSst)
            .OrderByDescending(incidente => incidente.Fecha)
            .ThenByDescending(incidente => incidente.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<IncidenteSstListadoDto> items = new List<IncidenteSstListadoDto>();
        foreach (IncidenteSst incidente in incidentes)
        {
            items.Add(ConvertirListado(incidente));
        }

        return new PaginaIncidentesSstDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<IncidenteSstDetalleDto> ObtenerAsync(int id)
    {
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, id);
        return ConvertirDetalle(incidente);
    }

    public async Task<IncidenteSstCreadoDto> RegistrarAsync(RegistrarIncidenteSstRequestDto solicitud)
    {
        IncidenteSst incidente = ValidacionSst.ValidarIncidente(solicitud);
        await ValidacionSst.ObtenerEmpleadoAsync(_unidad, incidente.EmpleadoId);
        incidente.Numero = ValidacionSst.NuevoNumeroTemporal();
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await _unidad.Repository<IncidenteSst>().AddAsync(incidente);
        await _unidad.CompleteAsync();
        incidente.Numero = ValidacionSst.NumeroDefinitivo(incidente.Id);
        _unidad.Repository<IncidenteSst>().Update(incidente);
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new IncidenteSstCreadoDto { Id = incidente.Id, Numero = incidente.Numero };
    }

    public async Task<AccionCorrectivaSstIdDto> AgregarAccionAsync(int id, GuardarAccionCorrectivaRequestDto solicitud)
    {
        AccionCorrectivaSst accion = ValidacionSst.ValidarAccion(solicitud);
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, id);
        ExigirAbierto(incidente);
        accion.IncidenteSstId = incidente.Id;
        await _unidad.Repository<AccionCorrectivaSst>().AddAsync(accion);
        await _unidad.CompleteAsync();
        return new AccionCorrectivaSstIdDto { Id = accion.Id };
    }

    public async Task<AccionCorrectivaSstIdDto> CompletarAccionAsync(
        int id,
        int accionId,
        CompletarAccionCorrectivaRequestDto solicitud)
    {
        DateOnly fecha = ValidacionSst.ValidarFechaNoFutura(solicitud.FechaCompletada);
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, id);
        ExigirAbierto(incidente);
        AccionCorrectivaSst? accion = incidente.AccionesCorrectivasSst.SingleOrDefault(otra => otra.Id == accionId);
        if (accion is null)
        {
            throw ValidacionSst.AccionInexistente(accionId);
        }

        if (accion.Completada)
        {
            throw ValidacionSst.AccionYaCompletada(accionId);
        }

        accion.Completada = true;
        accion.FechaCompletada = fecha;
        await _unidad.CompleteAsync();
        return new AccionCorrectivaSstIdDto { Id = accion.Id };
    }

    public async Task<IncidenteSstIdDto> CerrarAsync(int id)
    {
        IncidenteSst incidente = await ValidacionSst.ObtenerIncidenteAsync(_unidad, id);
        ExigirAbierto(incidente);
        if (incidente.AccionesCorrectivasSst.Count == 0)
        {
            throw ValidacionSst.Error409($"El incidente {incidente.Numero} necesita al menos una acción correctiva para cerrarse.");
        }

        int pendientes = incidente.AccionesCorrectivasSst.Count(accion => !accion.Completada);
        if (pendientes > 0)
        {
            throw ValidacionSst.Error409($"El incidente {incidente.Numero} tiene {pendientes} acciones correctivas sin completar.");
        }

        incidente.Estado = ValidacionSst.EstadoCerrado;
        incidente.FechaCierre = ValidacionSst.Hoy();
        await _unidad.CompleteAsync();
        return new IncidenteSstIdDto { Id = incidente.Id };
    }

    private static void ExigirAbierto(IncidenteSst incidente)
    {
        if (incidente.Estado != ValidacionSst.EstadoAbierto)
        {
            throw ValidacionSst.IncidenteCerrado(incidente.Numero);
        }
    }

    private static IncidenteSstListadoDto ConvertirListado(IncidenteSst incidente)
    {
        return new IncidenteSstListadoDto
        {
            Id = incidente.Id,
            Numero = incidente.Numero,
            EmpleadoId = incidente.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(incidente.Empleado),
            Tipo = incidente.Tipo,
            Severidad = incidente.Severidad,
            Fecha = incidente.Fecha,
            Lugar = incidente.Lugar,
            Descripcion = incidente.Descripcion,
            DiasBaja = incidente.DiasBaja,
            Estado = incidente.Estado,
            FechaCierre = incidente.FechaCierre,
            Acciones = incidente.AccionesCorrectivasSst.Count,
            AccionesPendientes = incidente.AccionesCorrectivasSst.Count(accion => !accion.Completada)
        };
    }

    private static IncidenteSstDetalleDto ConvertirDetalle(IncidenteSst incidente)
    {
        IncidenteSstDetalleDto detalle = new IncidenteSstDetalleDto
        {
            Id = incidente.Id,
            Numero = incidente.Numero,
            EmpleadoId = incidente.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(incidente.Empleado),
            Tipo = incidente.Tipo,
            Severidad = incidente.Severidad,
            Fecha = incidente.Fecha,
            Lugar = incidente.Lugar,
            Descripcion = incidente.Descripcion,
            DiasBaja = incidente.DiasBaja,
            Estado = incidente.Estado,
            FechaCierre = incidente.FechaCierre
        };
        foreach (AccionCorrectivaSst accion in incidente.AccionesCorrectivasSst.OrderBy(otra => otra.Id))
        {
            detalle.Acciones.Add(new AccionCorrectivaSstDto
            {
                Id = accion.Id,
                Descripcion = accion.Descripcion,
                Responsable = accion.Responsable,
                FechaCompromiso = accion.FechaCompromiso,
                Completada = accion.Completada,
                FechaCompletada = accion.FechaCompletada
            });
        }

        return detalle;
    }
}
