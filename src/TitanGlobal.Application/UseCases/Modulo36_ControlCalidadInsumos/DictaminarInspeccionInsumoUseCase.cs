using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class DictaminarInspeccionInsumoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionInsumo _escritura;

    public DictaminarInspeccionInsumoUseCase(IUnitOfWork unidad, IEscrituraInspeccionInsumo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, DictaminarInspeccionInsumoRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, DictaminarAsync);

        async Task<int> DictaminarAsync()
        {
            InspeccionInsumo inspeccion = await PersistenciaInspeccionInsumo.ObtenerAsync(_unidad, id);
            if (inspeccion.Estado != ValidacionInspeccionInsumo.Pendiente)
            {
                throw ValidacionInspeccionInsumo.Conflicto("Solo se pueden dictaminar inspecciones pendientes.");
            }

            string resultado = ValidacionInspeccionInsumo.NormalizarResultado(solicitud.Resultado);
            decimal defectuosa = ValidacionInspeccionInsumo.ValidarCantidadDefectuosa(
                solicitud.CantidadDefectuosa, inspeccion.CantidadMuestra);
            string? observaciones = ValidacionInspeccionInsumo.NormalizarObservaciones(solicitud.Observaciones);
            decimal porcentaje = ValidacionInspeccionInsumo.Porcentaje(defectuosa, inspeccion.CantidadMuestra) ?? 0m;
            if (resultado == ValidacionInspeccionInsumo.Aprobada && porcentaje > inspeccion.ToleranciaDefectosPorcentaje)
            {
                throw ValidacionInspeccionInsumo.ToleranciaSuperada(porcentaje, inspeccion.ToleranciaDefectosPorcentaje);
            }

            inspeccion.Estado = resultado;
            inspeccion.CantidadDefectuosa = defectuosa;
            inspeccion.Observaciones = observaciones;
            inspeccion.FechaDictamen = DateTime.UtcNow;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
