using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Commands;

public class CrearContratoLaboralCommand : IRequest<ContratoLaboralCreadoDto>
{
    public required GuardarContratoLaboralRequestDto Solicitud { get; set; }
}

public class CrearContratoLaboralCommandHandler : IRequestHandler<CrearContratoLaboralCommand, ContratoLaboralCreadoDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearContratoLaboralCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ContratoLaboralCreadoDto> Handle(CrearContratoLaboralCommand request, CancellationToken cancellationToken)
    {
        if (request.Solicitud.EmpleadoId is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El empleado es obligatorio.");
        }

        int empleadoId = request.Solicitud.EmpleadoId.Value;
        string modalidad = ValidacionContratoLaboral.ValidarModalidad(request.Solicitud.Modalidad);
        if (request.Solicitud.FechaInicio is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de inicio es obligatoria.");
        }

        DateOnly fechaInicio = request.Solicitud.FechaInicio.Value;
        decimal sueldo = ValidacionContratoLaboral.ValidarSueldo(request.Solicitud.SueldoBasico);
        string cargo = ValidacionContratoLaboral.ValidarCargo(request.Solicitud.Cargo);
        ValidacionContratoLaboral.ValidarPresenciaFechaFin(modalidad, request.Solicitud.FechaFin);
        ValidacionContratoLaboral.ValidarOrdenFechas(fechaInicio, request.Solicitud.FechaFin);
        Empleado? empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw ValidacionContratoLaboral.EmpleadoInexistente(empleadoId);
        }

        if (empleado.Estado == "Cesado")
        {
            throw ValidacionContratoLaboral.EmpleadoCesado(empleadoId);
        }

        if (fechaInicio < empleado.FechaIngreso)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de inicio no puede ser anterior a la fecha de ingreso del empleado.");
        }

        ContratoLaboral contrato = new ContratoLaboral
        {
            Numero = ValidacionContratoLaboral.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = fechaInicio,
            FechaFin = request.Solicitud.FechaFin,
            SueldoBasico = sueldo,
            Cargo = cargo,
            Estado = ValidacionContratoLaboral.EstadoVigente
        };
        DateOnly hoy = ValidacionContratoLaboral.Hoy();
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            List<ContratoLaboral> vigentes = await _unidad.Repository<ContratoLaboral>().AsQueryable()
                .Where(existente => existente.EmpleadoId == empleadoId
                    && existente.Estado == ValidacionContratoLaboral.EstadoVigente)
                .ToListAsync();
            foreach (ContratoLaboral vigente in vigentes.Where(existente => existente.FechaFin < hoy))
            {
                vigente.Estado = ValidacionContratoLaboral.EstadoVencido;
            }

            await _unidad.CompleteAsync();
            if (vigentes.Any(existente => existente.Estado == ValidacionContratoLaboral.EstadoVigente))
            {
                throw ValidacionContratoLaboral.ConflictoVigente(empleadoId);
            }

            await _unidad.Repository<ContratoLaboral>().AddAsync(contrato);
            await _unidad.CompleteAsync();
            contrato.Numero = ValidacionContratoLaboral.NumeroDefinitivo(contrato.Id);
            _unidad.Repository<ContratoLaboral>().Update(contrato);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsConflictoVigente(excepcion))
        {
            throw ValidacionContratoLaboral.ConflictoVigente(empleadoId);
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsEmpleadoInexistente(excepcion))
        {
            throw ValidacionContratoLaboral.EmpleadoInexistente(empleadoId);
        }

        return new ContratoLaboralCreadoDto { Id = contrato.Id, Numero = contrato.Numero };
    }
}
