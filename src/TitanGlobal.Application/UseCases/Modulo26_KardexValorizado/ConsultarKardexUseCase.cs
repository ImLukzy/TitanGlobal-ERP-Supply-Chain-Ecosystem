using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

public class ConsultarKardexUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ConsultarKardexUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KardexDto> ExecuteAsync(int productoId, int almacenId, string? metodo)
    {
        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == productoId);
        Producto producto = ValidacionKardex.ValidarProductoExistente(productoEncontrado, productoId);
        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == almacenId);
        Almacen almacen = ValidacionKardex.ValidarAlmacenExistente(almacenEncontrado, almacenId);
        string metodoNormalizado = ValidacionKardex.NormalizarMetodo(metodo);

        List<MovimientoKardex> movimientos = await _unitOfWork.Repository<MovimientoKardex>()
            .AsQueryable()
            .Where(registro => registro.ProductoId == producto.Id && registro.AlmacenId == almacen.Id)
            .OrderBy(registro => registro.Fecha)
            .ThenBy(registro => registro.Id)
            .ToListAsync();

        List<LineaKardexDto> lineas = metodoNormalizado == "PEPS"
            ? CalcularPeps(movimientos)
            : CalcularPromedio(movimientos);

        KardexDto resultado = new KardexDto
        {
            ProductoId = producto.Id,
            ProductoNombre = producto.Nombre,
            AlmacenId = almacen.Id,
            AlmacenCodigo = almacen.Codigo,
            Metodo = metodoNormalizado,
            Lineas = lineas
        };
        if (lineas.Count > 0)
        {
            LineaKardexDto ultima = lineas[lineas.Count - 1];
            resultado.SaldoCantidad = ultima.SaldoCantidad;
            resultado.SaldoValor = ultima.SaldoValor;
        }

        return resultado;
    }

    private static List<LineaKardexDto> CalcularPromedio(List<MovimientoKardex> movimientos)
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

    private static List<LineaKardexDto> CalcularPeps(List<MovimientoKardex> movimientos)
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
