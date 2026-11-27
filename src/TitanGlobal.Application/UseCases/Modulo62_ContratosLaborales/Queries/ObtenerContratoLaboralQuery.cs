using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Queries;

public class ObtenerContratoLaboralQuery : IRequest<ContratoLaboralDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerContratoLaboralQueryHandler : IRequestHandler<ObtenerContratoLaboralQuery, ContratoLaboralDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerContratoLaboralQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ContratoLaboralDetalleDto> Handle(ObtenerContratoLaboralQuery request, CancellationToken cancellationToken)
    {
        DateOnly hoy = ValidacionContratoLaboral.Hoy();
        ContratoLaboralDetalleDto? detalle = await _unidad.Repository<ContratoLaboral>().AsQueryable()
            .Where(contrato => contrato.Id == request.Id)
            .Select(contrato => new ContratoLaboralDetalleDto
            {
                Id = contrato.Id,
                Numero = contrato.Numero,
                EmpleadoId = contrato.EmpleadoId,
                EmpleadoLegajo = contrato.Empleado.Legajo,
                EmpleadoNombre = contrato.Empleado.Apellidos + ", " + contrato.Empleado.Nombres,
                Modalidad = contrato.Modalidad,
                FechaInicio = contrato.FechaInicio,
                FechaFin = contrato.FechaFin,
                SueldoBasico = contrato.SueldoBasico,
                Cargo = contrato.Cargo,
                Estado = contrato.Estado == ValidacionContratoLaboral.EstadoVigente
                    && contrato.FechaFin != null
                    && contrato.FechaFin < hoy
                    ? ValidacionContratoLaboral.EstadoVencido
                    : contrato.Estado,
                ContratoAnteriorId = contrato.ContratoAnteriorId,
                ContratoAnteriorNumero = contrato.ContratoAnterior != null ? contrato.ContratoAnterior.Numero : null,
                FechaCierre = contrato.FechaCierre,
                MotivoCierre = contrato.MotivoCierre
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El contrato laboral {request.Id} no existe.");
        }

        List<AdendaContratoDto> adendas = await _unidad.Repository<AdendaContrato>().AsQueryable()
            .Where(adenda => adenda.ContratoLaboralId == request.Id)
            .OrderBy(adenda => adenda.FechaVigencia)
            .Select(adenda => new AdendaContratoDto
            {
                Id = adenda.Id,
                FechaVigencia = adenda.FechaVigencia,
                Cargo = adenda.Cargo,
                SueldoBasico = adenda.SueldoBasico
            })
            .ToListAsync();
        AdendaContratoDto? vigente = adendas.LastOrDefault(adenda => adenda.FechaVigencia <= hoy);
        detalle.Adendas = adendas;
        detalle.CargoVigente = vigente?.Cargo ?? detalle.Cargo;
        detalle.SueldoVigente = vigente?.SueldoBasico ?? detalle.SueldoBasico;
        return detalle;
    }
}
