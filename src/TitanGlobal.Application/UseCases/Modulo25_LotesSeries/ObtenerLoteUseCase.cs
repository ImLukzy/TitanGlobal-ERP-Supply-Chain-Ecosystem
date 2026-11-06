using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class ObtenerLoteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerLoteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LoteDetalleDto> ExecuteAsync(int id)
    {
        DateTime hoy = DateTime.UtcNow.Date;
        LoteDetalleDto? lote = await _unitOfWork.Repository<Lote>()
            .AsQueryable()
            .Where(registro => registro.Id == id)
            .Select(registro => new LoteDetalleDto
            {
                Id = registro.Id,
                ProductoId = registro.ProductoId,
                ProductoNombre = registro.Producto.Nombre,
                Codigo = registro.Codigo,
                FechaFabricacion = registro.FechaFabricacion,
                FechaVencimiento = registro.FechaVencimiento,
                Cantidad = registro.Cantidad,
                Estado = registro.Estado,
                Vencido = registro.FechaVencimiento != null && registro.FechaVencimiento < hoy
            })
            .SingleOrDefaultAsync();
        if (lote is null)
        {
            throw ValidacionLoteSerie.CrearLoteNoEncontrado(id);
        }

        lote.TotalSeries = await _unitOfWork.Repository<UnidadSerie>()
            .AsQueryable()
            .CountAsync(registro => registro.LoteId == id);

        return lote;
    }
}
