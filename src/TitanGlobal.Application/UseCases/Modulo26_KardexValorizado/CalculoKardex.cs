using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

internal static class CalculoKardex
{
    public static List<LineaKardexDto> CalcularPromedio(List<MovimientoKardex> movimientos)
    {
        List<LineaKardexDto> lineas = new List<LineaKardexDto>();
        int saldoCantidad = 0;
        decimal saldoValor = 0m;
        foreach (MovimientoKardex movimiento in movimientos)
        {
            decimal costoUnitario;
            decimal valor;
            if (movimiento.Tipo == "Entrada")
            {
                costoUnitario = movimiento.CostoUnitario ?? 0m;
                valor = movimiento.Cantidad * costoUnitario;
                saldoCantidad += movimiento.Cantidad;
                saldoValor += valor;
            }
            else if (saldoCantidad <= 0)
            {
                costoUnitario = 0m;
                valor = 0m;
                saldoCantidad -= movimiento.Cantidad;
            }
            else
            {
                costoUnitario = saldoValor / saldoCantidad;
                valor = movimiento.Cantidad >= saldoCantidad ? saldoValor : movimiento.Cantidad * costoUnitario;
                saldoCantidad -= movimiento.Cantidad;
                saldoValor -= valor;
                if (saldoCantidad <= 0)
                {
                    saldoValor = 0m;
                }
            }

            lineas.Add(CrearLinea(movimiento, costoUnitario, valor, saldoCantidad, saldoValor));
        }

        return lineas;
    }

    public static List<LineaKardexDto> CalcularPeps(List<MovimientoKardex> movimientos)
    {
        List<LineaKardexDto> lineas = new List<LineaKardexDto>();
        List<int> capasCantidad = new List<int>();
        List<decimal> capasCosto = new List<decimal>();
        int saldoCantidad = 0;
        decimal saldoValor = 0m;
        foreach (MovimientoKardex movimiento in movimientos)
        {
            decimal costoUnitario;
            decimal valor;
            if (movimiento.Tipo == "Entrada")
            {
                costoUnitario = movimiento.CostoUnitario ?? 0m;
                valor = movimiento.Cantidad * costoUnitario;
                capasCantidad.Add(movimiento.Cantidad);
                capasCosto.Add(costoUnitario);
                saldoCantidad += movimiento.Cantidad;
                saldoValor += valor;
            }
            else
            {
                int pendiente = movimiento.Cantidad;
                valor = 0m;
                while (pendiente > 0 && capasCantidad.Count > 0)
                {
                    int tomar = Math.Min(pendiente, capasCantidad[0]);
                    valor += tomar * capasCosto[0];
                    capasCantidad[0] -= tomar;
                    pendiente -= tomar;
                    if (capasCantidad[0] == 0)
                    {
                        capasCantidad.RemoveAt(0);
                        capasCosto.RemoveAt(0);
                    }
                }

                costoUnitario = movimiento.Cantidad > 0 ? valor / movimiento.Cantidad : 0m;
                saldoCantidad -= movimiento.Cantidad;
                saldoValor -= valor;
            }

            lineas.Add(CrearLinea(movimiento, costoUnitario, valor, saldoCantidad, saldoValor));
        }

        return lineas;
    }

    private static LineaKardexDto CrearLinea(
        MovimientoKardex movimiento,
        decimal costoUnitario,
        decimal valor,
        int saldoCantidad,
        decimal saldoValor)
    {
        decimal costoPromedio = saldoCantidad > 0 ? saldoValor / saldoCantidad : 0m;

        return new LineaKardexDto
        {
            MovimientoId = movimiento.Id,
            Fecha = movimiento.Fecha,
            Tipo = movimiento.Tipo,
            Cantidad = movimiento.Cantidad,
            CostoUnitario = Math.Round(costoUnitario, 4),
            ValorMovimiento = Math.Round(valor, 2),
            SaldoCantidad = saldoCantidad,
            SaldoValor = Math.Round(saldoValor, 2),
            CostoPromedio = Math.Round(costoPromedio, 4)
        };
    }
}
