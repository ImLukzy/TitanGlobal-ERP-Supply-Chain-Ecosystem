using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo35_ConsumoMateriales;

public class ResumenConsumosMaterialesUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenConsumosMaterialesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenConsumosMaterialesDto> ExecuteAsync()
    {
        IQueryable<ConsumoMaterial> consulta = _unidad.Repository<ConsumoMaterial>().AsQueryable();
        IQueryable<ConsumoMaterial> registrados = consulta
            .Where(consumo => consumo.Estado == ValidacionConsumoMaterial.Registrado);

        DateOnly hoy = ValidacionConsumoMaterial.HoyEnPeru();
        DateTime inicio = ValidacionConsumoMaterial.InicioDelDiaEnPeru(hoy.AddDays(-6));
        DateTime limite = ValidacionConsumoMaterial.InicioDelDiaEnPeru(hoy.AddDays(1));

        int totalRegistrados = await registrados.CountAsync();
        int totalAnulados = await consulta.CountAsync(consumo => consumo.Estado == ValidacionConsumoMaterial.Anulado);

        // PATRÓN LINQ 11: valores únicos de las órdenes con consumo registrado.
        int ordenesConConsumo = await registrados
            .Select(consumo => consumo.OrdenProduccionId)
            .Distinct()
            .CountAsync();
        int ultimos7Dias = await registrados
            .CountAsync(consumo => consumo.FechaRegistro >= inicio && consumo.FechaRegistro < limite);

        return new ResumenConsumosMaterialesDto(totalRegistrados, totalAnulados, ordenesConConsumo, ultimos7Dias);
    }
}
