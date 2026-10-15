using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo02_SegmentacionClientes.UseCases;

public class EliminarSegmentoClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarSegmentoClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        SegmentoCliente? segmento = await _unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (segmento is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El segmento {id} no existe.");
        }

        _unitOfWork.Repository<SegmentoCliente>().Remove(segmento);
        await _unitOfWork.CompleteAsync();
    }
}
