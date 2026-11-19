using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

internal static class PersistenciaListaMaterial
{
    public const string IndiceVersion = "IX_ListasMateriales_ProductoId_Version";

    public const string IndiceActiva = "IX_ListasMateriales_ProductoId_Activa";

    public static async Task<ListaMateriales> ObtenerParaEscrituraAsync(IUnitOfWork unidad, int id)
    {
        ListaMateriales? lista = await unidad.Repository<ListaMateriales>().AsQueryable()
            .SingleOrDefaultAsync(lista => lista.Id == id);
        return lista ?? throw ValidacionListaMaterial.NoEncontrada(id);
    }

    public static async Task GuardarComponentesAsync(
        IUnitOfWork unidad,
        int productoId,
        List<DetalleListaMaterial> detalles)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            if (excepcion.Restriccion == "FK_ListasMateriales_Productos_ProductoId")
            {
                throw ValidacionListaMaterial.SolicitudInvalida($"El producto {productoId} no existe.");
            }

            if (excepcion.Restriccion == "FK_DetallesListaMaterial_Productos_ComponenteProductoId")
            {
                List<int> ids = detalles.Select(detalle => detalle.ComponenteProductoId).ToList();
                HashSet<int> existentes = await ValidacionListaMaterial.ConsultarProductosAsync(unidad, ids);
                int faltante = ids.FirstOrDefault(id => !existentes.Contains(id));
                if (faltante != 0)
                {
                    throw ValidacionListaMaterial.SolicitudInvalida($"El producto componente {faltante} no existe.");
                }
            }

            throw;
        }
    }
}
