using MediatR;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales.Commands;

public class RenovarContratoLaboralCommand : IRequest<ContratoLaboralCreadoDto>
{
    public int Id { get; set; }

    public required RenovarContratoLaboralRequestDto Solicitud { get; set; }
}

public class RenovarContratoLaboralCommandHandler : IRequestHandler<RenovarContratoLaboralCommand, ContratoLaboralCreadoDto>
{
    private readonly IUnitOfWork _unidad;

    public RenovarContratoLaboralCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ContratoLaboralCreadoDto> Handle(RenovarContratoLaboralCommand request, CancellationToken cancellationToken)
    {
        string modalidad = ValidacionContratoLaboral.ValidarModalidad(request.Solicitud.Modalidad);
        decimal sueldo = ValidacionContratoLaboral.ValidarSueldo(request.Solicitud.SueldoBasico);
        string cargo = ValidacionContratoLaboral.ValidarCargo(request.Solicitud.Cargo);
        ValidacionContratoLaboral.ValidarPresenciaFechaFin(modalidad, request.Solicitud.FechaFin);
        ContratoLaboral anterior = await ValidacionContratoLaboral.ObtenerAsync(_unidad, request.Id);
        if (anterior.Estado != ValidacionContratoLaboral.EstadoVigente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {anterior.Numero} no está vigente y no se puede renovar.");
        }

        if (anterior.FechaFin is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El contrato {anterior.Numero} es indeterminado y no se renueva.");
        }

        int empleadoId = anterior.EmpleadoId;
        Empleado empleado = await _unidad.Repository<Empleado>().AsQueryable()
            .SingleAsync(entidad => entidad.Id == empleadoId);
        if (empleado.Estado == "Cesado")
        {
            throw ValidacionContratoLaboral.EmpleadoCesado(empleadoId);
        }

        DateOnly fechaInicio = anterior.FechaFin.Value.AddDays(1);
        ValidacionContratoLaboral.ValidarOrdenFechas(fechaInicio, request.Solicitud.FechaFin);
        ContratoLaboral nuevo = new ContratoLaboral
        {
            Numero = ValidacionContratoLaboral.NuevoNumeroTemporal(),
            EmpleadoId = empleadoId,
            Modalidad = modalidad,
            FechaInicio = fechaInicio,
            FechaFin = request.Solicitud.FechaFin,
            SueldoBasico = sueldo,
            Cargo = cargo,
            Estado = ValidacionContratoLaboral.EstadoVigente,
            ContratoAnteriorId = anterior.Id
        };
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        try
        {
            anterior.Estado = ValidacionContratoLaboral.EstadoRenovado;
            await _unidad.CompleteAsync();
            await _unidad.Repository<ContratoLaboral>().AddAsync(nuevo);
            await _unidad.CompleteAsync();
            nuevo.Numero = ValidacionContratoLaboral.NumeroDefinitivo(nuevo.Id);
            _unidad.Repository<ContratoLaboral>().Update(nuevo);
            await _unidad.CompleteAsync();
            transaccion.Complete();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionContratoLaboral.EsConflictoVigente(excepcion))
        {
            throw ValidacionContratoLaboral.ConflictoVigente(empleadoId);
        }

        return new ContratoLaboralCreadoDto { Id = nuevo.Id, Numero = nuevo.Numero };
    }
}
