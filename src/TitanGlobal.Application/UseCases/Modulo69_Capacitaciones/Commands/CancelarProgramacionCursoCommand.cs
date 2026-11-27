using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class CancelarProgramacionCursoCommand : IRequest<ProgramacionIdDto>
{
    public int Id { get; set; }
}

public class CancelarProgramacionCursoCommandHandler : IRequestHandler<CancelarProgramacionCursoCommand, ProgramacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CancelarProgramacionCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ProgramacionIdDto> Handle(CancelarProgramacionCursoCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        List<InscripcionCurso> inscripciones = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Where(inscripcion => inscripcion.ProgramacionCursoId == request.Id
                && inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado)
            .ToListAsync();
        foreach (InscripcionCurso inscripcion in inscripciones)
        {
            inscripcion.Estado = ValidacionCapacitacion.InscripcionRetirado;
        }

        programacion.Estado = ValidacionCapacitacion.ProgramacionCancelada;
        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }
}
