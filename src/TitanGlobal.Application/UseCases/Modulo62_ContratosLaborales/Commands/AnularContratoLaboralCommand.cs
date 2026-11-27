using MediatR;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Commands;

public class AnularContratoLaboralCommand : IRequest<ContratoLaboralIdDto>
{
    public int Id { get; set; }

    public required AnularContratoLaboralRequestDto Solicitud { get; set; }
}

public class AnularContratoLaboralCommandHandler : IRequestHandler<AnularContratoLaboralCommand, ContratoLaboralIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AnularContratoLaboralCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ContratoLaboralIdDto> Handle(AnularContratoLaboralCommand request, CancellationToken cancellationToken)
    {
        string motivo = ValidacionContratoLaboral.ValidarMotivoAnulacion(request.Solicitud.Motivo);
        ContratoLaboral contrato = await ValidacionContratoLaboral.ObtenerAsync(_unidad, request.Id);
        if (contrato.Estado != ValidacionContratoLaboral.EstadoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {contrato.Numero} no está vigente y no se puede anular.");
        }

        contrato.Estado = ValidacionContratoLaboral.EstadoAnulado;
        contrato.MotivoCierre = motivo;
        contrato.FechaCierre = ValidacionContratoLaboral.Hoy();
        await _unidad.CompleteAsync();
        return new ContratoLaboralIdDto { Id = contrato.Id };
    }
}
