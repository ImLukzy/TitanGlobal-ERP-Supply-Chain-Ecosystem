using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo39_MantenimientoPreventivo;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

public class ListarMantenimientosPreventivosUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarMantenimientosPreventivosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaMantenimientosPreventivosDto> ExecuteAsync(
        string? busqueda, int? centroTrabajoId, string? estado, DateOnly? desde, DateOnly? hasta,
        int pagina, int tamanoPagina)
    {
        ValidacionMantenimientoPreventivo.ValidarPaginacion(pagina, tamanoPagina);
        string? termino = ValidacionMantenimientoPreventivo.NormalizarBusqueda(busqueda);
        IQueryable<MantenimientoPreventivo> consulta = _unidad.Repository<MantenimientoPreventivo>().AsQueryable();
        if (termino != null)
        {
            // PATRÓN LINQ 1: filtro de texto insensible a mayúsculas.
            consulta = consulta.Where(mantenimiento => mantenimiento.Numero.ToLower().Contains(termino)
                || mantenimiento.Descripcion.ToLower().Contains(termino));
        }

        if (centroTrabajoId.HasValue)
        {
            ValidacionMantenimientoPreventivo.ValidarIdCentro(centroTrabajoId.Value);
            consulta = consulta.Where(mantenimiento => mantenimiento.CentroTrabajoId == centroTrabajoId.Value);
        }

        if (estado != null)
        {
            string estadoNormalizado = ValidacionMantenimientoPreventivo.NormalizarEstado(estado);
            consulta = consulta.Where(mantenimiento => mantenimiento.Estado == estadoNormalizado);
        }

        ValidacionMantenimientoPreventivo.ValidarRango(desde, hasta);
        // PATRÓN LINQ 6: filtro por rango de fecha.
        if (desde.HasValue)
        {
            consulta = consulta.Where(mantenimiento => mantenimiento.FechaProgramada >= desde.Value);
        }

        if (hasta.HasValue)
        {
            consulta = consulta.Where(mantenimiento => mantenimiento.FechaProgramada <= hasta.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        if (inicio >= total)
        {
            return new PaginaMantenimientosPreventivosDto(
                new List<MantenimientoPreventivoListadoDto>(), total, pagina, tamanoPagina);
        }

        DateOnly hoy = ValidacionMantenimientoPreventivo.Hoy();
        // PATRÓN LINQ 3: proyección directa a DTO.
        List<MantenimientoPreventivoListadoDto> items = await consulta
            .OrderBy(mantenimiento => mantenimiento.FechaProgramada)
            .ThenBy(mantenimiento => mantenimiento.Id)
            .Skip((int)inicio)
            .Take(tamanoPagina)
            .Select(mantenimiento => new MantenimientoPreventivoListadoDto(
                mantenimiento.Id,
                mantenimiento.Numero,
                mantenimiento.CentroTrabajoId,
                mantenimiento.CentroTrabajo.Codigo,
                mantenimiento.CentroTrabajo.Nombre,
                mantenimiento.Descripcion,
                mantenimiento.FechaProgramada,
                mantenimiento.DuracionHorasPlanificada,
                mantenimiento.Estado,
                mantenimiento.Estado == ValidacionMantenimientoPreventivo.Programado
                    && mantenimiento.FechaProgramada < hoy))
            .ToListAsync();
        return new PaginaMantenimientosPreventivosDto(items, total, pagina, tamanoPagina);
    }
}
