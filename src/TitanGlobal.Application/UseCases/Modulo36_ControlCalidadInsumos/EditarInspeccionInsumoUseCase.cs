using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class EditarInspeccionInsumoUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraInspeccionInsumo _escritura;

    public EditarInspeccionInsumoUseCase(IUnitOfWork unidad, IEscrituraInspeccionInsumo escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, GuardarInspeccionInsumoRequestDto solicitud)
    {
        return await _escritura.EjecutarAsync(id, EditarAsync);

        async Task<int> EditarAsync()
        {
            InspeccionInsumo inspeccion = await PersistenciaInspeccionInsumo.ObtenerAsync(_unidad, id);
            if (inspeccion.Estado != ValidacionInspeccionInsumo.Pendiente)
            {
                throw ValidacionInspeccionInsumo.Conflicto("Solo se pueden editar inspecciones pendientes.");
            }

            DatosInspeccionInsumo datos = DatosInspeccionInsumo.Validar(solicitud);
            await PersistenciaInspeccionInsumo.ComprobarReferenciasAsync(_unidad, datos.ProductoId, datos.ProveedorId);
            await PersistenciaInspeccionInsumo.ComprobarLoteLibreAsync(
                _unidad, datos.ProductoId, datos.ProveedorId, datos.Lote, id);
            datos.Aplicar(inspeccion);
            await PersistenciaInspeccionInsumo.GuardarAsync(_unidad, datos.ProductoId, datos.ProveedorId, datos.Lote);
            return id;
        }
    }
}
