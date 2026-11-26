using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class TerminarOrdenProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public TerminarOrdenProduccionUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, TerminarOrdenProduccionRequestDto solicitud)
    {
        return await _escritura.EjecutarBloqueandoAsync(id, TerminarAsync);

        async Task<int> TerminarAsync()
        {
            OrdenProduccion orden = await ValidacionOrdenProduccion.ObtenerAsync(_unidad, id);
            if (orden.Estado != ValidacionOrdenProduccion.EnProceso)
            {
                throw ValidacionOrdenProduccion.Conflicto("Solo se pueden terminar órdenes de producción en proceso.");
            }

            int cantidad = solicitud.CantidadProducida ?? -1;
            if (cantidad < 0 || cantidad > ValidacionOrdenProduccion.MaximoUnidades)
            {
                throw ValidacionOrdenProduccion.Invalida("La cantidad producida debe estar entre 0 y 1000000.");
            }

            orden.Estado = ValidacionOrdenProduccion.Terminada;
            orden.CantidadProducida = cantidad;
            orden.FechaFinReal = DateTime.UtcNow;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
