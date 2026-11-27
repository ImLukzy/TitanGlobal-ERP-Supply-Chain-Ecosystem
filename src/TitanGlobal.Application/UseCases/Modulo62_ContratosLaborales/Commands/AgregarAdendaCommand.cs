using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Commands;

public class AgregarAdendaCommand : IRequest<ContratoLaboralIdDto>
{
    public int Id { get; set; }

    public required AdendaContratoRequestDto Solicitud { get; set; }
}

public class AgregarAdendaCommandHandler : IRequestHandler<AgregarAdendaCommand, ContratoLaboralIdDto>
{
    private readonly IUnitOfWork _unidad;

    public AgregarAdendaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ContratoLaboralIdDto> Handle(AgregarAdendaCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.FechaVigencia is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de vigencia es obligatoria.");
        }

        DateOnly fechaVigencia = request.Solicitud.FechaVigencia.Value;
        string? cargoNuevo = string.IsNullOrWhiteSpace(request.Solicitud.Cargo) ? null : request.Solicitud.Cargo.Trim();
        if (cargoNuevo is null && request.Solicitud.SueldoBasico is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La adenda debe cambiar el cargo o el sueldo básico.");
        }

        if (request.Solicitud.SueldoBasico is not null)
        {
            ValidacionContratoLaboral.ValidarSueldoPresente(request.Solicitud.SueldoBasico.Value);
        }

        if (cargoNuevo is not null)
        {
            ValidacionContratoLaboral.ValidarLongitudCargo(cargoNuevo);
        }

        ContratoLaboral contrato = await ValidacionContratoLaboral.ObtenerAsync(_unidad, request.Id);
        if (contrato.Estado != ValidacionContratoLaboral.EstadoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {contrato.Numero} no está vigente y no admite adendas.");
        }

        if (fechaVigencia < contrato.FechaInicio || (contrato.FechaFin is not null && fechaVigencia > contrato.FechaFin.Value))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de vigencia debe estar dentro del periodo del contrato.");
        }

        List<AdendaContrato> adendas = await _unidad.Repository<AdendaContrato>().AsQueryable()
            .Where(adenda => adenda.ContratoLaboralId == request.Id)
            .OrderBy(adenda => adenda.FechaVigencia)
            .ToListAsync();
        if (adendas.Any(adenda => adenda.FechaVigencia >= fechaVigencia))
        {
            throw ValidacionContratoLaboral.ConflictoAdenda();
        }

        AdendaContrato? ultima = adendas.LastOrDefault();
        string cargoVigente = ultima?.Cargo ?? contrato.Cargo;
        decimal sueldoVigente = ultima?.SueldoBasico ?? contrato.SueldoBasico;
        string cargoResultante = cargoNuevo ?? cargoVigente;
        decimal sueldoResultante = request.Solicitud.SueldoBasico ?? sueldoVigente;
        if (cargoResultante == cargoVigente && sueldoResultante == sueldoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La adenda no cambia el cargo ni el sueldo vigentes en esa fecha.");
        }

        AdendaContrato adendaNueva = new AdendaContrato
        {
            ContratoLaboralId = request.Id,
            FechaVigencia = fechaVigencia,
            Cargo = cargoResultante,
            SueldoBasico = sueldoResultante
        };
        try
        {
            await _unidad.Repository<AdendaContrato>().AddAsync(adendaNueva);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsConflictoAdenda(excepcion))
        {
            throw ValidacionContratoLaboral.ConflictoAdenda();
        }

        return new ContratoLaboralIdDto { Id = adendaNueva.Id };
    }
}
