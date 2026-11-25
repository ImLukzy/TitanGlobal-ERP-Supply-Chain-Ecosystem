using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

internal static class PersistenciaRutaProduccion
{
    public const string IndiceVersion = "IX_RutasProduccion_ProductoId_Version";
    public const string IndiceActiva = "IX_RutasProduccion_ProductoId_Activa";
    public const string ReferenciaCentro = "FK_OperacionesRutaProduccion_CentrosTrabajo_CentroTrabajoId";

    public static async Task<RutaProduccion> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        RutaProduccion? ruta = await unidad.Repository<RutaProduccion>().AsQueryable()
            .SingleOrDefaultAsync(ruta => ruta.Id == id);
        return ruta ?? throw ValidacionRutaProduccion.NoEncontrada(id);
    }

    public static async Task GuardarOperacionesAsync(
        IUnitOfWork unidad, int productoId, List<OperacionRutaProduccion> operaciones)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && error.Restriccion == "FK_RutasProduccion_Productos_ProductoId")
        {
            throw ValidacionRutaProduccion.ProductoInexistente(productoId);
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && error.Restriccion == ReferenciaCentro)
        {
            List<int> ids = operaciones.Select(operacion => operacion.CentroTrabajoId).Distinct().ToList();
            HashSet<int> existentes = await ValidacionRutaProduccion.ConsultarCentrosAsync(unidad, ids);
            int faltante = ids.FirstOrDefault(id => !existentes.Contains(id));
            if (faltante > 0)
            {
                throw ValidacionRutaProduccion.CentroInexistente(faltante);
            }

            throw;
        }
    }
}
