using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas.Commands;

public class RestaurarAlertaCommand : IRequest
{
    public required ClaveAlertaRequestDto Solicitud { get; set; }
}

public class RestaurarAlertaCommandHandler : IRequestHandler<RestaurarAlertaCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public RestaurarAlertaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RestaurarAlertaCommand request, CancellationToken cancellationToken)
    {
        string clave = OperacionesAlertas.ValidarClave(request.Solicitud.Clave);
        AlertaDescartada? descartada = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .SingleOrDefaultAsync(alerta => alerta.Clave == clave);
        if (descartada is null)
        {
            throw new ReglaNegocioException(404, $"La alerta {clave} no está descartada.");
        }

        _unitOfWork.Repository<AlertaDescartada>().Remove(descartada);
        await _unitOfWork.CompleteAsync();
    }
}
