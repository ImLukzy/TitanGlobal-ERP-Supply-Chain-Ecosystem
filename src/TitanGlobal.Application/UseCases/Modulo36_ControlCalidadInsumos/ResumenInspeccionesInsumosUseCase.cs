using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class ResumenInspeccionesInsumosUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenInspeccionesInsumosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenInspeccionesInsumosDto> ExecuteAsync()
    {
        IQueryable<InspeccionInsumo> inspecciones = _unidad.Repository<InspeccionInsumo>().AsQueryable();
        int total = await inspecciones.CountAsync();
        // PATRÓN LINQ 9: agrupación con orden explícito del ciclo de estados.
        List<EstadoInspeccionesInsumosDto> porEstado = await inspecciones
            .GroupBy(inspeccion => inspeccion.Estado)
            .OrderBy(grupo => grupo.Key == ValidacionInspeccionInsumo.Pendiente ? 0
                : grupo.Key == ValidacionInspeccionInsumo.Aprobada ? 1
                : grupo.Key == ValidacionInspeccionInsumo.Rechazada ? 2 : 3)
            .Select(grupo => new EstadoInspeccionesInsumosDto(grupo.Key, grupo.Count()))
            .ToListAsync();

        // PATRÓN LINQ 9: agrupación por proveedor con sus conteos, agregada en la consulta.
        List<DictamenesProveedorInsumo> dictamenes = await inspecciones
            .Where(inspeccion => inspeccion.Estado == ValidacionInspeccionInsumo.Aprobada
                || inspeccion.Estado == ValidacionInspeccionInsumo.Rechazada)
            .GroupBy(inspeccion => new { inspeccion.ProveedorId, inspeccion.Proveedor.RazonSocial })
            .Select(grupo => new DictamenesProveedorInsumo(
                grupo.Key.ProveedorId,
                grupo.Key.RazonSocial,
                grupo.Count(inspeccion => inspeccion.Estado == ValidacionInspeccionInsumo.Aprobada),
                grupo.Count(inspeccion => inspeccion.Estado == ValidacionInspeccionInsumo.Rechazada)))
            .ToListAsync();

        List<ProveedorInspeccionesInsumosDto> porProveedor = dictamenes
            .Select(fila => new ProveedorInspeccionesInsumosDto(
                fila.ProveedorId,
                fila.RazonSocial,
                fila.Aprobadas,
                fila.Rechazadas,
                ValidacionInspeccionInsumo.TasaRechazo(fila.Aprobadas, fila.Rechazadas)))
            .OrderByDescending(fila => fila.TasaRechazoPorcentaje)
            .ThenBy(fila => fila.ProveedorRazonSocial, StringComparer.Ordinal)
            .ToList();
        int aprobadas = porProveedor.Sum(fila => fila.Aprobadas);
        int rechazadas = porProveedor.Sum(fila => fila.Rechazadas);
        return new ResumenInspeccionesInsumosDto(
            total, porEstado, ValidacionInspeccionInsumo.TasaRechazo(aprobadas, rechazadas), porProveedor);
    }
}
