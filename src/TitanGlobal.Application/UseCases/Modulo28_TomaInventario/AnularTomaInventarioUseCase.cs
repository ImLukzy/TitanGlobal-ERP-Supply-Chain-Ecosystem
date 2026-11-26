using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

public class AnularTomaInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AnularTomaInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TomaInventarioDetalleDto> ExecuteAsync(int id)
    {
        TomaInventario? tomaEncontrada = await _unitOfWork.Repository<TomaInventario>()
            .AsQueryable()
            .Include(registro => registro.Almacen)
            .Include(registro => registro.DetallesTomaInventario)
                .ThenInclude(detalle => detalle.Producto)
            .SingleOrDefaultAsync(registro => registro.Id == id);
        TomaInventario toma = ValidacionTomaInventario.ValidarTomaExistente(tomaEncontrada, id);
        ValidacionTomaInventario.ValidarEstadoAbierta(toma);

        toma.Estado = "Anulada";
        _unitOfWork.Repository<TomaInventario>().Update(toma);
        await _unitOfWork.CompleteAsync();

        return ValidacionTomaInventario.MapearDetalle(toma);
    }
}
