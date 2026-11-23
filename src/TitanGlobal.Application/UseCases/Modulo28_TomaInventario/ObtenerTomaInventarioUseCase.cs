using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

public class ObtenerTomaInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerTomaInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TomaInventarioDetalleDto> ExecuteAsync(int id)
    {
        TomaInventario? tomaEncontrada = await _unitOfWork.Repository<TomaInventario>()
            .AsQueryable()
            .AsNoTracking()
            .Include(registro => registro.Almacen)
            .Include(registro => registro.DetallesTomaInventario)
                .ThenInclude(detalle => detalle.Producto)
            .SingleOrDefaultAsync(registro => registro.Id == id);
        TomaInventario toma = ValidacionTomaInventario.ValidarTomaExistente(tomaEncontrada, id);

        return ValidacionTomaInventario.MapearDetalle(toma);
    }
}
