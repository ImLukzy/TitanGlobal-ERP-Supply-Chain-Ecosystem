using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Commands;

public class EliminarCotizacionCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarCotizacionCommandHandler : IRequestHandler<EliminarCotizacionCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarCotizacionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarCotizacionCommand request, CancellationToken cancellationToken)
    {
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (cotizacion is null)
        {
            throw OperacionesCotizacion.CrearNoEncontrada(request.Id);
        }

        OperacionesCotizacion.ExigirEmitida(cotizacion);
        _unitOfWork.Repository<Cotizacion>().Remove(cotizacion);
        await _unitOfWork.CompleteAsync();
    }
}
