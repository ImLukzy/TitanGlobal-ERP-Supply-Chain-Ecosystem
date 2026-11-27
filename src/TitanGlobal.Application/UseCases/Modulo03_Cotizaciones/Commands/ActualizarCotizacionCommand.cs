using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Commands;

public class ActualizarCotizacionCommand : IRequest<CotizacionCreadaDto>
{
    public int Id { get; set; }

    public required GuardarCotizacionRequestDto Solicitud { get; set; }
}

public class ActualizarCotizacionCommandHandler : IRequestHandler<ActualizarCotizacionCommand, CotizacionCreadaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarCotizacionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionCreadaDto> Handle(ActualizarCotizacionCommand request, CancellationToken cancellationToken)
    {
        CotizacionNormalizada datos = await ValidacionCotizacion.ValidarAsync(request.Solicitud, _unitOfWork);
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (cotizacion is null)
        {
            throw OperacionesCotizacion.CrearNoEncontrada(request.Id);
        }

        OperacionesCotizacion.ExigirEmitida(cotizacion);
        List<DetalleCotizacion> anteriores = await _unitOfWork.Repository<DetalleCotizacion>()
            .AsQueryable()
            .Where(detalle => detalle.CotizacionId == request.Id)
            .ToListAsync();
        foreach (DetalleCotizacion detalle in anteriores)
        {
            _unitOfWork.Repository<DetalleCotizacion>().Remove(detalle);
        }

        cotizacion.ClienteId = request.Solicitud.ClienteId;
        cotizacion.FechaValidez = datos.FechaValidez;
        cotizacion.Observaciones = datos.Observaciones;
        foreach (LineaCotizacionRequestDto linea in datos.Lineas)
        {
            await _unitOfWork.Repository<DetalleCotizacion>().AddAsync(new DetalleCotizacion
            {
                CotizacionId = cotizacion.Id,
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                DescuentoPorcentaje = linea.DescuentoPorcentaje
            });
        }

        _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
        await _unitOfWork.CompleteAsync();
        return new CotizacionCreadaDto
        {
            Id = cotizacion.Id,
            Numero = cotizacion.Numero
        };
    }
}
