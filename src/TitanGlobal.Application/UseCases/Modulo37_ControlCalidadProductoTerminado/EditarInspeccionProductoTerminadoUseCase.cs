using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo37_ControlCalidadProductoTerminado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo37_ControlCalidadProductoTerminado;

public class EditarInspeccionProductoTerminadoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionProductoTerminado _escritura;

    public EditarInspeccionProductoTerminadoUseCase(IUnitOfWork unidad, IEscrituraInspeccionProductoTerminado escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, EditarInspeccionProductoTerminadoRequestDto solicitud)
    {
        int? ordenId = await _unidad.Repository<InspeccionProductoTerminado>().AsQueryable()
            .Where(inspeccion => inspeccion.Id == id)
            .Select(inspeccion => (int?)inspeccion.OrdenProduccionId)
            .SingleOrDefaultAsync();
        if (!ordenId.HasValue)
        {
            throw ValidacionInspeccionProductoTerminado.NoEncontrada(id);
        }

        // Se bloquean la orden (unidades disponibles) y la inspección (frente al dictamen o la anulación).
        return await _escritura.EjecutarBloqueandoOrdenAsync(ordenId.Value, id, EditarAsync);

        async Task<int> EditarAsync()
        {
            InspeccionProductoTerminado inspeccion = await ValidacionInspeccionProductoTerminado.ObtenerAsync(_unidad, id);
            if (inspeccion.Estado != ValidacionInspeccionProductoTerminado.Pendiente)
            {
                throw ValidacionInspeccionProductoTerminado.Conflicto("Solo se pueden editar inspecciones pendientes.");
            }

            DatosInspeccion datos = ValidacionInspeccionProductoTerminado.ValidarDatos(
                solicitud.FechaInspeccion,
                solicitud.CantidadInspeccionada,
                solicitud.ToleranciaNoConformesPorcentaje,
                solicitud.Inspector);

            // En la edición, las unidades disponibles excluyen las de la propia inspección.
            await ValidacionInspeccionProductoTerminado.ValidarOrdenAsync(
                _unidad, inspeccion.OrdenProduccionId, datos, inspeccion.Id);
            inspeccion.FechaInspeccion = datos.FechaInspeccion;
            inspeccion.CantidadInspeccionada = datos.CantidadInspeccionada;
            inspeccion.ToleranciaNoConformesPorcentaje = datos.Tolerancia;
            inspeccion.Inspector = datos.Inspector;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
