using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class AnularInspeccionProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionProductoTerminado _escritura;

    public AnularInspeccionProductoTerminadoUseCase(
        IUnitOfWork unidad, IEscrituraInspeccionProductoTerminado escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, AnularInspeccionProductoTerminadoRequestDto solicitud)
    {
        return await _escritura.EjecutarBloqueandoInspeccionAsync(id, AnularAsync);

        async Task<int> AnularAsync()
        {
            InspeccionProductoTerminado inspeccion = await ValidacionInspeccionProductoTerminado.ObtenerAsync(_unidad, id);
            if (inspeccion.Estado != ValidacionInspeccionProductoTerminado.Pendiente)
            {
                throw ValidacionInspeccionProductoTerminado.Conflicto("Solo se pueden anular inspecciones pendientes.");
            }

            string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
            if (motivo.Length == 0)
            {
                throw ValidacionInspeccionProductoTerminado.Invalida("El motivo de anulación es obligatorio.");
            }

            if (motivo.Length > 250)
            {
                throw ValidacionInspeccionProductoTerminado.Invalida(
                    "El motivo de anulación no puede superar 250 caracteres.");
            }

            inspeccion.Estado = ValidacionInspeccionProductoTerminado.Anulada;
            inspeccion.MotivoAnulacion = motivo;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
