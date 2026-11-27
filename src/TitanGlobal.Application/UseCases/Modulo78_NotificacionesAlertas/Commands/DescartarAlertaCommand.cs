using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas.Commands;

public class DescartarAlertaCommand : IRequest
{
    public required ClaveAlertaRequestDto Solicitud { get; set; }
}

public class DescartarAlertaCommandHandler : IRequestHandler<DescartarAlertaCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DescartarAlertaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DescartarAlertaCommand request, CancellationToken cancellationToken)
    {
        string clave = OperacionesAlertas.ValidarClave(request.Solicitud.Clave);
        List<AlertaDto> alertas = await OperacionesAlertas.ObtenerAlertasAsync(_unitOfWork);
        if (!alertas.Any(alerta => alerta.Clave == clave))
        {
            throw new ReglaNegocioException(404, $"La alerta {clave} no existe o ya no está activa.");
        }

        AlertaDescartada? existente = await _unitOfWork.Repository<AlertaDescartada>()
            .AsQueryable()
            .SingleOrDefaultAsync(alerta => alerta.Clave == clave);
        if (existente is not null)
        {
            throw new ReglaNegocioException(409, $"La alerta {clave} ya está descartada.");
        }

        AlertaDescartada descartada = new AlertaDescartada
        {
            Clave = clave,
            FechaDescarte = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };
        try
        {
            await _unitOfWork.Repository<AlertaDescartada>().AddAsync(descartada);
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsClaveDuplicada(excepcion))
        {
            throw new ReglaNegocioException(409, $"La alerta {clave} ya está descartada.");
        }
    }

    private static bool EsClaveDuplicada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
