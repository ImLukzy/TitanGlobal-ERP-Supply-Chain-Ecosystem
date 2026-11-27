using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class ActualizarTurnoCommand : IRequest<TurnoIdDto>
{
    public int Id { get; set; }

    public required GuardarTurnoRequestDto Solicitud { get; set; }
}

public class ActualizarTurnoCommandHandler : IRequestHandler<ActualizarTurnoCommand, TurnoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<TurnoIdDto> Handle(ActualizarTurnoCommand request, CancellationToken cancellationToken)
    {
        Turno nuevo = ValidacionTurno.ValidarTurno(request.Solicitud);
        Turno turno = await ValidacionTurno.ObtenerTurnoAsync(_unidad, request.Id);
        bool conAsignaciones = await _unidad.Repository<AsignacionTurno>().AsQueryable()
            .AnyAsync(asignacion => asignacion.TurnoId == request.Id);
        if (conAsignaciones)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El turno {turno.Codigo} tiene asignaciones y no se puede modificar. Crea un turno nuevo.");
        }

        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<Turno>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != request.Id);
        if (existe)
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        turno.Codigo = nuevo.Codigo;
        turno.Nombre = nuevo.Nombre;
        turno.Tipo = nuevo.Tipo;
        turno.HoraEntrada = nuevo.HoraEntrada;
        turno.HoraSalida = nuevo.HoraSalida;
        turno.CruzaMedianoche = nuevo.CruzaMedianoche;
        turno.ToleranciaMinutos = nuevo.ToleranciaMinutos;
        turno.DiasLaborables = nuevo.DiasLaborables;
        turno.DiasTrabajo = nuevo.DiasTrabajo;
        turno.DiasDescanso = nuevo.DiasDescanso;
        turno.FechaInicioCiclo = nuevo.FechaInicioCiclo;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionTurno.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        return new TurnoIdDto { Id = turno.Id };
    }
}
