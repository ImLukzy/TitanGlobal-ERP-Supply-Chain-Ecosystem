using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo03_Cotizaciones.Commands;

public class CrearCotizacionCommand : IRequest<CotizacionCreadaDto>
{
    public required GuardarCotizacionRequestDto Solicitud { get; set; }
}

public class CrearCotizacionCommandHandler : IRequestHandler<CrearCotizacionCommand, CotizacionCreadaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCotizacionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionCreadaDto> Handle(CrearCotizacionCommand request, CancellationToken cancellationToken)
    {
        CotizacionNormalizada datos = await ValidacionCotizacion.ValidarAsync(request.Solicitud, _unitOfWork);
        Cotizacion cotizacion = new Cotizacion
        {
            Numero = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}",
            Fecha = FechasUtc.Normalizar(DateTime.UtcNow),
            ClienteId = request.Solicitud.ClienteId,
            FechaValidez = datos.FechaValidez,
            Estado = "Emitida",
            Observaciones = datos.Observaciones
        };
        foreach (LineaCotizacionRequestDto linea in datos.Lineas)
        {
            cotizacion.Detalles.Add(new DetalleCotizacion
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                DescuentoPorcentaje = linea.DescuentoPorcentaje
            });
        }

        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await _unitOfWork.Repository<Cotizacion>().AddAsync(cotizacion);
            await _unitOfWork.CompleteAsync();
            cotizacion.Numero = CrearNumero(cotizacion.Id);
            _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
            await _unitOfWork.CompleteAsync();
            transaccion.Complete();
        }

        return new CotizacionCreadaDto
        {
            Id = cotizacion.Id,
            Numero = cotizacion.Numero
        };
    }

    private static string CrearNumero(int id)
    {
        return $"COT-{id:D6}";
    }
}
