using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

internal static class CalculoCarga
{
    public static ConsolidacionDetalleDto Detalle(ConsolidacionCarga carga)
    {
        ConsolidacionDetalleDto dto = new ConsolidacionDetalleDto { Id = carga.Id, Codigo = carga.Codigo, Nombre = carga.Nombre, Estado = carga.Estado, Observaciones = carga.Observaciones, FechaConfirmacion = carga.FechaConfirmacion, MotivoCancelacion = carga.MotivoCancelacion, FechaCancelacion = carga.FechaCancelacion };
        decimal volumen = 0; decimal peso = 0;
        foreach (LineaConsolidacionCarga linea in carga.Lineas.OrderBy(l => l.Posicion).ThenBy(l => l.Id))
        {
            int bultos = linea.UnidadesPorBulto == 0 ? 0 : (linea.Cantidad + linea.UnidadesPorBulto - 1) / linea.UnidadesPorBulto;
            decimal volumenLinea = decimal.Round(bultos * linea.LargoCm * linea.AnchoCm * linea.AltoCm / 1000000m, 6, MidpointRounding.AwayFromZero);
            decimal pesoLinea = bultos * linea.PesoBultoKg;
            volumen += volumenLinea; peso += pesoLinea;
            dto.TotalUnidades += linea.Cantidad; dto.UnidadesRecogidas += linea.CantidadRecogida; dto.TotalBultos += bultos;
            dto.Lineas.Add(new LineaCargaDto
            {
                Id = linea.Id,
                Posicion = linea.Posicion,
                Sku = linea.Sku,
                Descripcion = linea.Descripcion,
                Bultos = bultos,
                Cantidad = linea.Cantidad,
                CantidadRecogida = linea.CantidadRecogida,
                UnidadesPorBulto = linea.UnidadesPorBulto,
                LargoCm = linea.LargoCm.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                AnchoCm = linea.AnchoCm.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                AltoCm = linea.AltoCm.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                PesoBultoKg = linea.PesoBultoKg.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                VolumenM3 = volumenLinea.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture),
                PesoKg = pesoLinea.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            });
        }
        dto.VolumenM3 = volumen.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
        dto.PesoKg = peso.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        return dto;
    }
}
