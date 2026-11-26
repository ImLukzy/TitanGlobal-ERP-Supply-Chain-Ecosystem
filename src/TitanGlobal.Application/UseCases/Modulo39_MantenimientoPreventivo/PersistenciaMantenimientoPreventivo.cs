using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo39_MantenimientoPreventivo;

internal static class PersistenciaMantenimientoPreventivo
{
    public const string IndiceFecha = "IX_MantenimientosPreventivos_CentroTrabajoId_FechaProgramada";
    public const string ReferenciaCentro = "FK_MantenimientosPreventivos_CentrosTrabajo_CentroTrabajoId";

    public static async Task<MantenimientoPreventivo> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        MantenimientoPreventivo? mantenimiento = await unidad.Repository<MantenimientoPreventivo>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        return mantenimiento ?? throw ValidacionMantenimientoPreventivo.NoEncontrado(id);
    }

    public static async Task<string> ObtenerCodigoCentroAsync(IUnitOfWork unidad, int centroTrabajoId)
    {
        return await unidad.Repository<CentroTrabajo>().AsQueryable()
            .Where(centro => centro.Id == centroTrabajoId)
            .Select(centro => centro.Codigo)
            .SingleAsync();
    }

    public static async Task ComprobarFechaLibreAsync(
        IUnitOfWork unidad, int centroTrabajoId, string codigoCentro, DateOnly fecha, int? excluirId)
    {
        bool ocupada = await unidad.Repository<MantenimientoPreventivo>().AsQueryable()
            .AnyAsync(otro => otro.CentroTrabajoId == centroTrabajoId
                && otro.FechaProgramada == fecha
                && otro.Estado != ValidacionMantenimientoPreventivo.Cancelado
                && (!excluirId.HasValue || otro.Id != excluirId.Value));
        if (ocupada)
        {
            throw ValidacionMantenimientoPreventivo.FechaOcupada(codigoCentro, fecha);
        }
    }

    public static async Task GuardarAsync(IUnitOfWork unidad, int centroTrabajoId, string codigoCentro, DateOnly fecha)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.Unicidad && error.Restriccion == IndiceFecha)
        {
            throw ValidacionMantenimientoPreventivo.FechaOcupada(codigoCentro, fecha);
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea && error.Restriccion == ReferenciaCentro)
        {
            throw ValidacionMantenimientoPreventivo.CentroInexistente(centroTrabajoId);
        }
    }
}
