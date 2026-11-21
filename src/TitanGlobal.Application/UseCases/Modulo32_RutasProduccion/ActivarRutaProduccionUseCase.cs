using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

public class ActivarRutaProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraRutaProduccion _escritura;

    public ActivarRutaProduccionUseCase(IUnitOfWork unidad, IEscrituraRutaProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        return await _escritura.EjecutarAsync(id, ActivarAsync);

        async Task<int> ActivarAsync()
        {
            RutaProduccion ruta = await PersistenciaRutaProduccion.ObtenerAsync(_unidad, id);
            if (ruta.Estado != ValidacionRutaProduccion.Borrador)
            {
                throw ValidacionRutaProduccion.Conflicto("Solo se pueden activar rutas de producción en borrador.");
            }

            int? inactivo = await _unidad.Repository<OperacionRutaProduccion>().AsQueryable()
                .Where(operacion => operacion.RutaProduccionId == id && !operacion.CentroTrabajo.Activo)
                .OrderBy(operacion => operacion.Secuencia)
                .Select(operacion => (int?)operacion.CentroTrabajoId)
                .FirstOrDefaultAsync();
            if (inactivo.HasValue)
            {
                throw ValidacionRutaProduccion.Conflicto($"El centro de trabajo {inactivo.Value} está inactivo.");
            }

            bool tieneActiva = await _unidad.Repository<RutaProduccion>().AsQueryable()
                .AnyAsync(otra => otra.ProductoId == ruta.ProductoId && otra.Estado == ValidacionRutaProduccion.Activa);
            if (tieneActiva)
            {
                throw RutaActiva(ruta.ProductoId);
            }

            ruta.Estado = ValidacionRutaProduccion.Activa;
            try
            {
                await _unidad.CompleteAsync();
            }
            catch (ViolacionRestriccionException error) when (
                error.Tipo == TipoViolacionRestriccion.Unicidad && error.Restriccion == PersistenciaRutaProduccion.IndiceActiva)
            {
                throw RutaActiva(ruta.ProductoId);
            }

            return id;
        }
    }

    private static ReglaNegocioException RutaActiva(int productoId)
    {
        return ValidacionRutaProduccion.Conflicto($"El producto {productoId} ya tiene una ruta de producción activa.");
    }
}
