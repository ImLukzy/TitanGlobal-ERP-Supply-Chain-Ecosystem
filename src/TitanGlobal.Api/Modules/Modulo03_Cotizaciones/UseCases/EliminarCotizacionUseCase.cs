using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

public class EliminarCotizacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarCotizacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int id)
    {
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (cotizacion is null)
        {
            throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
        }

        ActualizarCotizacionUseCase.ExigirEmitida(cotizacion);
        _unitOfWork.Repository<Cotizacion>().Remove(cotizacion);
        await _unitOfWork.CompleteAsync();
    }
}
