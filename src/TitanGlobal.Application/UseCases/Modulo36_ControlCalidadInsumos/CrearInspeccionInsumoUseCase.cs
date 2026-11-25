using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

public class CrearInspeccionInsumoUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearInspeccionInsumoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<InspeccionInsumoCreadoDto> ExecuteAsync(GuardarInspeccionInsumoRequestDto solicitud)
    {
        DatosInspeccionInsumo datos = DatosInspeccionInsumo.Validar(solicitud);
        await PersistenciaInspeccionInsumo.ComprobarReferenciasAsync(_unidad, datos.ProductoId, datos.ProveedorId);
        await PersistenciaInspeccionInsumo.ComprobarLoteLibreAsync(
            _unidad, datos.ProductoId, datos.ProveedorId, datos.Lote, null);

        InspeccionInsumo inspeccion = new InspeccionInsumo
        {
            Numero = ValidacionInspeccionInsumo.NuevoNumeroTemporal(),
            Estado = ValidacionInspeccionInsumo.Pendiente
        };
        datos.Aplicar(inspeccion);

        // El número definitivo sale del Id: las dos escrituras van en la misma transacción.
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await _unidad.Repository<InspeccionInsumo>().AddAsync(inspeccion);
            await PersistenciaInspeccionInsumo.GuardarAsync(_unidad, datos.ProductoId, datos.ProveedorId, datos.Lote);
            inspeccion.Numero = ValidacionInspeccionInsumo.NumeroDefinitivo(inspeccion.Id);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }

        return new InspeccionInsumoCreadoDto(inspeccion.Id, inspeccion.Numero);
    }
}
