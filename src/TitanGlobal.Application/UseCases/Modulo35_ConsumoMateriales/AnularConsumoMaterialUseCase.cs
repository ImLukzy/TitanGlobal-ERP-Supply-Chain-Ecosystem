using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

public class AnularConsumoMaterialUseCase
{
    private readonly IUnitOfWork _unidad;
    private readonly IEscrituraOrdenProduccion _escritura;

    public AnularConsumoMaterialUseCase(IUnitOfWork unidad, IEscrituraOrdenProduccion escritura)
    {
        _unidad = unidad;
        _escritura = escritura;
    }

    public async Task<int> ExecuteAsync(int id, AnularConsumoMaterialRequestDto solicitud)
    {
        int? ordenId = await _unidad.Repository<ConsumoMaterial>().AsQueryable()
            .Where(consumo => consumo.Id == id)
            .Select(consumo => (int?)consumo.OrdenProduccionId)
            .SingleOrDefaultAsync();
        if (!ordenId.HasValue)
        {
            throw ValidacionConsumoMaterial.NoEncontrado(id);
        }

        // Se bloquea la orden, como en el registro, para que no termine mientras se anula.
        return await _escritura.EjecutarBloqueandoAsync(ordenId.Value, AnularAsync);

        async Task<int> AnularAsync()
        {
            ConsumoMaterial consumo = await _unidad.Repository<ConsumoMaterial>().AsQueryable()
                .Include(existente => existente.OrdenProduccion)
                .SingleAsync(existente => existente.Id == id);
            if (consumo.Estado == ValidacionConsumoMaterial.Anulado)
            {
                throw ValidacionConsumoMaterial.Conflicto($"El consumo de materiales {consumo.Numero} ya está anulado.");
            }

            if (consumo.OrdenProduccion.Estado != ValidacionConsumoMaterial.OrdenEnProceso)
            {
                throw ValidacionConsumoMaterial.Conflicto(
                    "Solo se pueden anular consumos de órdenes de producción en proceso.");
            }

            string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
            if (motivo.Length == 0)
            {
                throw ValidacionConsumoMaterial.Invalida("El motivo de anulación es obligatorio.");
            }

            if (motivo.Length > 250)
            {
                throw ValidacionConsumoMaterial.Invalida("El motivo de anulación no puede superar 250 caracteres.");
            }

            consumo.Estado = ValidacionConsumoMaterial.Anulado;
            consumo.MotivoAnulacion = motivo;
            consumo.FechaAnulacion = DateTime.UtcNow;
            await _unidad.CompleteAsync();
            return id;
        }
    }
}
