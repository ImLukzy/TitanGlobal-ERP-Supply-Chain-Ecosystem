using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class EliminarLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(id);
        }

        bool tieneOfertas = await _unitOfWork.Repository<OfertaLicitacion>()
            .AsQueryable()
            .AnyAsync(oferta => oferta.LicitacionId == id);
        if (tieneOfertas)
        {
            throw ValidacionLicitacion.Conflicto(
                $"La licitación {id} tiene ofertas y no se puede eliminar; cancélela.");
        }

        _unitOfWork.Repository<Licitacion>().Remove(licitacion);
        await _unitOfWork.CompleteAsync();
    }
}
