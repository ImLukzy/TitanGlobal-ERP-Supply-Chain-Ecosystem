using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

internal static class CalculoVale
{
    public static ValeListadoDto Listado(ValeCombustible vale)
    {
        ValeListadoDto dto = new ValeListadoDto();
        Asignar(vale, dto);
        return dto;
    }

    public static ValeDetalleDto Detalle(ValeCombustible vale)
    {
        ValeDetalleDto dto = new ValeDetalleDto
        {
            Observaciones = vale.Observaciones,
            FechaConfirmacion = vale.FechaConfirmacion,
            FechaAnulacion = vale.FechaAnulacion,
            MotivoAnulacion = vale.MotivoAnulacion
        };
        Asignar(vale, dto);
        return dto;
    }

    private static void Asignar(ValeCombustible vale, ValeListadoDto dto)
    {
        dto.Id = vale.Id;
        dto.Codigo = vale.Codigo;
        dto.Placa = vale.Placa;
        dto.Estacion = vale.Estacion;
        dto.FechaCarga = vale.FechaCarga;
        dto.TipoCombustible = vale.TipoCombustible;
        dto.Litros = vale.Litros.ToString("F3", CultureInfo.InvariantCulture);
        dto.PrecioLitro = vale.PrecioLitro.ToString("F3", CultureInfo.InvariantCulture);
        dto.Moneda = vale.Moneda;
        dto.OdometroInicial = vale.OdometroInicial;
        dto.OdometroFinal = vale.OdometroFinal;
        dto.Estado = vale.Estado;
        dto.Importe = decimal.Round(vale.Litros * vale.PrecioLitro, 2, MidpointRounding.AwayFromZero).ToString("F2", CultureInfo.InvariantCulture);
        if (vale.OdometroFinal.HasValue)
        {
            int distancia = vale.OdometroFinal.Value - vale.OdometroInicial;
            dto.KilometrosRecorridos = distancia;
            dto.KmPorLitro = decimal.Round(distancia / vale.Litros, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
            dto.LitrosPor100Km = decimal.Round(vale.Litros * 100 / distancia, 3, MidpointRounding.AwayFromZero).ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}
