using TitanGlobal.Application.DTOs.Modulo34_OrdenesProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

public class CancelarOrdenProduccionUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public CancelarOrdenProduccionUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, CancelarOrdenProduccionRequestDto solicitud)
    {
        return await _escritura.EjecutarBloqueandoAsync(id, CancelarAsync);

        async Task<int> CancelarAsync()
        {
            OrdenProduccion orden = await ValidacionOrdenProduccion.ObtenerAsync(_unidad, id);
            if (orden.Estado != ValidacionOrdenProduccion.Planificada && orden.Estado != ValidacionOrdenProduccion.Liberada)
            {
                throw ValidacionOrdenProduccion.Conflicto(
                    "Solo se pueden cancelar órdenes de producción planificadas o liberadas.");
            }

            string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
            if (motivo.Length == 0)
            {
                throw ValidacionOrdenProduccion.Invalida("El motivo de cancelación es obligatorio.");
            }

            if (motivo.Length > 250)
            {
                throw ValidacionOrdenProduccion.Invalida("El motivo de cancelación no puede superar 250 caracteres.");
            }

            orden.Estado = ValidacionOrdenProduccion.Cancelada;
            orden.MotivoCancelacion = motivo;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
