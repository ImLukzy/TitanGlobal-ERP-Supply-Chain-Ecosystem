using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class CrearInspeccionProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionProductoTerminado _escritura;

    public CrearInspeccionProductoTerminadoUseCase(IUnitOfWork unidad, IEscrituraInspeccionProductoTerminado escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<InspeccionProductoTerminadoCreadaDto> ExecuteAsync(CrearInspeccionProductoTerminadoRequestDto solicitud)
    {
        int ordenId = solicitud.OrdenProduccionId;
        ValidacionInspeccionProductoTerminado.ValidarIdOrden(ordenId);
        DatosInspeccion datos = ValidacionInspeccionProductoTerminado.ValidarDatos(
            solicitud.FechaInspeccion,
            solicitud.CantidadInspeccionada,
            solicitud.ToleranciaNoConformesPorcentaje,
            solicitud.Inspector);

        InspeccionProductoTerminado inspeccion = new InspeccionProductoTerminado
        {
            Numero = ValidacionInspeccionProductoTerminado.NumeroTemporal(),
            OrdenProduccionId = ordenId,
            FechaInspeccion = datos.FechaInspeccion,
            CantidadInspeccionada = datos.CantidadInspeccionada,
            ToleranciaNoConformesPorcentaje = datos.Tolerancia,
            Inspector = datos.Inspector,
            Estado = ValidacionInspeccionProductoTerminado.Pendiente
        };

        // El bloqueo de la orden impide que dos altas concurrentes superen juntas la cantidad producida.
        int id = await _escritura.EjecutarBloqueandoOrdenAsync(ordenId, null, CrearAsync);
        return new InspeccionProductoTerminadoCreadaDto(id, inspeccion.Numero);

        async Task<int> CrearAsync()
        {
            await ValidacionInspeccionProductoTerminado.ValidarOrdenAsync(_unidad, ordenId, datos, null);
            await _unidad.Repository<InspeccionProductoTerminado>().AddAsync(inspeccion);
            try
            {
                await _unidad.CompleteAsync();
            }
            catch (ViolacionRestriccionException error) when (
                error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
                && error.Restriccion == ValidacionInspeccionProductoTerminado.ReferenciaOrden)
            {
                throw ValidacionInspeccionProductoTerminado.Invalida($"La orden de producción {ordenId} no existe.");
            }

            // El número definitivo sale del Id, como OP-000001 en el módulo 34.
            inspeccion.Numero = ValidacionInspeccionProductoTerminado.NumeroDefinitivo(inspeccion.Id);
            await _unidad.CompleteAsync();
            return inspeccion.Id;
        }
    }
}
