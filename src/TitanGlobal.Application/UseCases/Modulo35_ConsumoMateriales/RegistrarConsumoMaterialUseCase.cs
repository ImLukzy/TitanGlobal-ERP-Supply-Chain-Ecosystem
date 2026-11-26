using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

public class RegistrarConsumoMaterialUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public RegistrarConsumoMaterialUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<ConsumoMaterialRegistradoDto> ExecuteAsync(RegistrarConsumoMaterialRequestDto solicitud)
    {
        int ordenId = solicitud.OrdenProduccionId;
        int componenteId = solicitud.ComponenteProductoId;
        ValidacionConsumoMaterial.ValidarIdOrden(ordenId);
        ValidacionConsumoMaterial.ValidarIdComponente(componenteId);
        decimal cantidad = ValidacionConsumoMaterial.ValidarCantidad(solicitud.Cantidad);
        string? responsable = ValidacionConsumoMaterial.NormalizarTextoOpcional(
            solicitud.Responsable, 100, "El responsable no puede superar 100 caracteres.");
        string? observaciones = ValidacionConsumoMaterial.NormalizarTextoOpcional(
            solicitud.Observaciones, 250, "Las observaciones no pueden superar 250 caracteres.");

        ConsumoMaterial consumo = new ConsumoMaterial
        {
            Numero = ValidacionConsumoMaterial.NumeroTemporal(),
            OrdenProduccionId = ordenId,
            ComponenteProductoId = componenteId,
            Cantidad = cantidad,
            Responsable = responsable,
            Observaciones = observaciones,
            Estado = ValidacionConsumoMaterial.Registrado
        };

        // El bloqueo de la orden evita registrar un consumo mientras la orden pasa a Terminada.
        int id = await _escritura.EjecutarBloqueandoAsync(ordenId, RegistrarAsync);
        return new ConsumoMaterialRegistradoDto(id, consumo.Numero);

        async Task<int> RegistrarAsync()
        {
            OrdenProduccion? orden = await _unidad.Repository<OrdenProduccion>().AsQueryable()
                .SingleOrDefaultAsync(existente => existente.Id == ordenId);
            if (orden == null)
            {
                throw ValidacionConsumoMaterial.OrdenInexistenteEnRegistro(ordenId);
            }

            if (orden.Estado != ValidacionConsumoMaterial.OrdenEnProceso)
            {
                throw ValidacionConsumoMaterial.Conflicto(
                    "Solo se pueden registrar consumos en órdenes de producción en proceso.");
            }

            // PATRÓN LINQ 5: la línea de la lista fija de la orden aporta la unidad de medida.
            string? unidad = await _unidad.Repository<DetalleListaMaterial>().AsQueryable()
                .Where(detalle => detalle.ListaMaterialId == orden.ListaMaterialesId
                    && detalle.ComponenteProductoId == componenteId)
                .Select(detalle => detalle.UnidadMedida)
                .FirstOrDefaultAsync();
            if (unidad == null)
            {
                throw ValidacionConsumoMaterial.ComponenteFueraDeLista(componenteId, orden.Numero);
            }

            consumo.UnidadMedida = unidad;
            consumo.FechaRegistro = DateTime.UtcNow;
            await _unidad.Repository<ConsumoMaterial>().AddAsync(consumo);
            try
            {
                await _unidad.CompleteAsync();
            }
            catch (ViolacionRestriccionException error) when (
                error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
                && error.Restriccion == ValidacionConsumoMaterial.ReferenciaOrden)
            {
                throw ValidacionConsumoMaterial.OrdenInexistenteEnRegistro(ordenId);
            }
            catch (ViolacionRestriccionException error) when (
                error.Tipo == TipoViolacionRestriccion.ReferenciaForanea
                && error.Restriccion == ValidacionConsumoMaterial.ReferenciaComponente)
            {
                throw ValidacionConsumoMaterial.ComponenteFueraDeLista(componenteId, orden.Numero);
            }

            // El número definitivo sale del Id, como OP-000001 en el módulo 34.
            consumo.Numero = ValidacionConsumoMaterial.NumeroDefinitivo(consumo.Id);
            await _unidad.CompleteAsync();
            return consumo.Id;
        }
    }
}
