using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

internal static class CalculoNotaCreditoDebito
{
    public static async Task<List<DetalleNotaCreditoDebito>> CalcularAsync(
        IUnitOfWork unitOfWork, int facturaId, List<DetalleNotaCreditoDebitoRequestDto> entradas)
    {
        int[] ids = entradas.Select(entrada => entrada.DetalleFacturaId).ToArray();
        Dictionary<int, DetalleFactura> origen = await unitOfWork.Repository<DetalleFactura>()
            .AsQueryable()
            .AsNoTracking()
            .Where(detalle => ids.Contains(detalle.Id))
            .ToDictionaryAsync(detalle => detalle.Id);

        if (origen.Count != entradas.Count)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound, "El detalle de factura indicado no existe.");
        }

        if (origen.Values.Any(detalle => detalle.FacturaId != facturaId))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest, "El detalle indicado no pertenece a la factura de la nota.");
        }

        List<DetalleNotaCreditoDebito> detalles = new();
        foreach (DetalleNotaCreditoDebitoRequestDto entrada in entradas)
        {
            detalles.Add(CalcularLinea(entrada, origen[entrada.DetalleFacturaId].PorcentajeImpuesto));
        }

        return detalles;
    }

    public static void AsignarTotales(NotaCreditoDebito nota, List<DetalleNotaCreditoDebito> detalles)
    {
        decimal baseImponible = detalles.Sum(detalle => detalle.BaseImponible);
        decimal impuesto = detalles.Sum(detalle => detalle.ImporteImpuesto);
        decimal total = baseImponible + impuesto;
        if (total <= 0 || total > ValidacionNotaCreditoDebito.ImporteMaximo)
        {
            throw ValidacionNotaCreditoDebito.ImporteInvalido();
        }

        nota.BaseImponible = baseImponible;
        nota.ImporteImpuesto = impuesto;
        nota.Total = total;
    }

    private static DetalleNotaCreditoDebito CalcularLinea(
        DetalleNotaCreditoDebitoRequestDto entrada, decimal tasaHistorica)
    {
        // Limitar el producto antes de multiplicar decimales de escalas distintas.
        decimal baseMaxima = ValidacionNotaCreditoDebito.ImporteMaximo + 0.005m;
        if (entrada.Cantidad > baseMaxima / entrada.PrecioUnitario)
        {
            throw ValidacionNotaCreditoDebito.ImporteInvalido();
        }

        decimal baseImponible = Redondear(entrada.Cantidad * entrada.PrecioUnitario);
        decimal impuesto = Redondear(baseImponible * tasaHistorica / 100m);
        decimal total = baseImponible + impuesto;
        if (total > ValidacionNotaCreditoDebito.ImporteMaximo)
        {
            throw ValidacionNotaCreditoDebito.ImporteInvalido();
        }

        return new DetalleNotaCreditoDebito
        {
            DetalleFacturaId = entrada.DetalleFacturaId,
            Cantidad = entrada.Cantidad,
            PrecioUnitario = entrada.PrecioUnitario,
            PorcentajeImpuesto = tasaHistorica,
            BaseImponible = baseImponible,
            ImporteImpuesto = impuesto,
            Total = total
        };
    }

    private static decimal Redondear(decimal importe)
    {
        return decimal.Round(importe, 2, MidpointRounding.AwayFromZero);
    }
}
