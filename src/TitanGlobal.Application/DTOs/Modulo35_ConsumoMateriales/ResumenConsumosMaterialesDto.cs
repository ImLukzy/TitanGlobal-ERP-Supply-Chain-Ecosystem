namespace TitanGlobal.Application.DTOs.Modulo35_ConsumoMateriales;

public sealed record ResumenConsumosMaterialesDto(
    int TotalRegistrados,
    int TotalAnulados,
    int OrdenesConConsumo,
    int ConsumosUltimos7Dias);
