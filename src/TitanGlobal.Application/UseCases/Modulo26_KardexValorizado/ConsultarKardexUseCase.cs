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
            ? CalculoKardex.CalcularPeps(movimientos)
            : CalculoKardex.CalcularPromedio(movimientos);

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
}
