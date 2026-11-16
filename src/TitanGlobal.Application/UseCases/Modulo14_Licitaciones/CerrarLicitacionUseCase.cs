using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class CerrarLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CerrarLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(id);
        }

        if (licitacion.Estado != ValidacionLicitacion.Abierta)
        {
            throw ValidacionLicitacion.NoAbierta(id);
        }

        licitacion.Estado = ValidacionLicitacion.Cerrada;
        await _unitOfWork.CompleteAsync();

        return licitacion.Id;
    }
}
