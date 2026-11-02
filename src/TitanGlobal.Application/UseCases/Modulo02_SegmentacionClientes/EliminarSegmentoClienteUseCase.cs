using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El segmento {id} no existe.");
        }

        _unitOfWork.Repository<SegmentoCliente>().Remove(segmento);
        await _unitOfWork.CompleteAsync();
    }
}
