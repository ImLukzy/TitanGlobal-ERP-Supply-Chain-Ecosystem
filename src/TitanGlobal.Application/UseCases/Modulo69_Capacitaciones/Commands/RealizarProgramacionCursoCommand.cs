using MediatR;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class RealizarProgramacionCursoCommand : IRequest<ProgramacionIdDto>
{
    public int Id { get; set; }
}

public class RealizarProgramacionCursoCommandHandler : IRequestHandler<RealizarProgramacionCursoCommand, ProgramacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RealizarProgramacionCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ProgramacionIdDto> Handle(RealizarProgramacionCursoCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        if (programacion.FechaFin > ValidacionCapacitacion.Hoy())
        {
            string fechaFin = ValidacionCapacitacion.Fecha(programacion.FechaFin);
            throw ValidacionCapacitacion.Error409($"La programación {request.Id} termina el {fechaFin} y aún no se puede marcar como realizada.");
        }

        programacion.Estado = ValidacionCapacitacion.ProgramacionRealizada;
        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }
}
