using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

public class ListarSaldosKardexUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarSaldosKardexUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SaldoKardexDto>> ExecuteAsync(int? almacenId)
    {
        IQueryable<MovimientoKardex> consulta = _unitOfWork.Repository<MovimientoKardex>().AsQueryable();
        if (almacenId.HasValue)
        {
            int almacenFiltro = almacenId.Value;
            consulta = consulta.Where(registro => registro.AlmacenId == almacenFiltro);
        }

        List<SaldoKardexDto> saldos = await consulta
            .GroupBy(registro => new { registro.ProductoId, registro.AlmacenId })
            .Select(grupo => new SaldoKardexDto
            {
                ProductoId = grupo.Key.ProductoId,
                AlmacenId = grupo.Key.AlmacenId,
                Entradas = grupo.Sum(registro => registro.Tipo == "Entrada" ? registro.Cantidad : 0),
                Salidas = grupo.Sum(registro => registro.Tipo == "Salida" ? registro.Cantidad : 0)
            })
            .ToListAsync();

        List<int> productoIds = saldos.Select(saldo => saldo.ProductoId).Distinct().ToList();
        List<int> almacenIds = saldos.Select(saldo => saldo.AlmacenId).Distinct().ToList();
        Dictionary<int, string> productos = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => productoIds.Contains(producto.Id))
            .ToDictionaryAsync(producto => producto.Id, producto => producto.Nombre);
        Dictionary<int, string> almacenes = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .Where(almacen => almacenIds.Contains(almacen.Id))
            .ToDictionaryAsync(almacen => almacen.Id, almacen => almacen.Codigo);

        foreach (SaldoKardexDto saldo in saldos)
        {
            saldo.ProductoNombre = productos[saldo.ProductoId];
            saldo.AlmacenCodigo = almacenes[saldo.AlmacenId];
            saldo.Saldo = saldo.Entradas - saldo.Salidas;
        }

        return saldos
            .OrderBy(saldo => saldo.ProductoId)
            .ThenBy(saldo => saldo.AlmacenId)
            .ToList();
    }
}
