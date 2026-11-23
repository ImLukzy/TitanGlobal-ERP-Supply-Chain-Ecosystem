using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

public class ObtenerAjusteInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerAjusteInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AjusteInventarioItemDto> ExecuteAsync(int id)
    {
        AjusteInventario? encontrado = await _unitOfWork.Repository<AjusteInventario>()
            .AsQueryable()
            .Include(registro => registro.Almacen)
            .Include(registro => registro.Producto)
            .SingleOrDefaultAsync(registro => registro.Id == id);
        AjusteInventario ajuste = ValidacionAjustesInventario.ValidarAjusteExistente(encontrado, id);

        return ValidacionAjustesInventario.MapearItem(ajuste);
    }
}
