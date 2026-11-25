using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

internal static class PersistenciaInspeccionInsumo
{
    public const string IndiceLote = "IX_InspeccionesInsumos_ProductoId_ProveedorId_Lote";
    public const string ReferenciaProducto = "FK_InspeccionesInsumos_Productos_ProductoId";
    public const string ReferenciaProveedor = "FK_InspeccionesInsumos_Proveedores_ProveedorId";

    public static async Task<InspeccionInsumo> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        InspeccionInsumo? inspeccion = await unidad.Repository<InspeccionInsumo>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        return inspeccion ?? throw ValidacionInspeccionInsumo.NoEncontrada(id);
    }

    public static async Task ComprobarReferenciasAsync(IUnitOfWork unidad, int productoId, int proveedorId)
    {
        bool productoExiste = await unidad.Repository<Producto>().AsQueryable()
            .AnyAsync(producto => producto.Id == productoId);
        if (!productoExiste)
        {
            throw ValidacionInspeccionInsumo.ProductoInexistente(productoId);
        }

        bool? proveedorActivo = await unidad.Repository<Proveedor>().AsQueryable()
            .Where(proveedor => proveedor.Id == proveedorId)
            .Select(proveedor => (bool?)proveedor.Activo)
            .SingleOrDefaultAsync();
        if (!proveedorActivo.HasValue)
        {
            throw ValidacionInspeccionInsumo.ProveedorInexistente(proveedorId);
        }

        if (!proveedorActivo.Value)
        {
            throw ValidacionInspeccionInsumo.ProveedorInactivo(proveedorId);
        }
    }

    public static async Task ComprobarLoteLibreAsync(
        IUnitOfWork unidad, int productoId, int proveedorId, string lote, int? excluirId)
    {
        bool repetido = await unidad.Repository<InspeccionInsumo>().AsQueryable()
            .AnyAsync(otra => otra.ProductoId == productoId
                && otra.ProveedorId == proveedorId
                && otra.Lote == lote
                && otra.Estado != ValidacionInspeccionInsumo.Anulada
                && (!excluirId.HasValue || otra.Id != excluirId.Value));
        if (repetido)
        {
            throw ValidacionInspeccionInsumo.LoteRepetido(lote);
        }
    }

    public static async Task GuardarAsync(IUnitOfWork unidad, int productoId, int proveedorId, string lote)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.Unicidad && error.Restriccion == IndiceLote)
        {
            throw ValidacionInspeccionInsumo.LoteRepetido(lote);
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea && error.Restriccion == ReferenciaProducto)
        {
            throw ValidacionInspeccionInsumo.ProductoInexistente(productoId);
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea && error.Restriccion == ReferenciaProveedor)
        {
            throw ValidacionInspeccionInsumo.ProveedorInexistente(proveedorId);
        }
    }
}
