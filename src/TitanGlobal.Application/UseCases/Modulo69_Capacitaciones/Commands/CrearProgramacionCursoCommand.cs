using MediatR;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class CrearProgramacionCursoCommand : IRequest<ProgramacionIdDto>
{
    public required GuardarProgramacionRequestDto Solicitud { get; set; }
}

public class CrearProgramacionCursoCommandHandler : IRequestHandler<CrearProgramacionCursoCommand, ProgramacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearProgramacionCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ProgramacionIdDto> Handle(CrearProgramacionCursoCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = ValidacionCapacitacion.ValidarProgramacion(request.Solicitud);
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, request.Solicitud.CursoId ?? 0);
        if (!curso.Activo)
        {
            throw ValidacionCapacitacion.CursoInactivo(curso.Codigo);
        }

        programacion.CursoId = curso.Id;
        await _unidad.Repository<ProgramacionCurso>().AddAsync(programacion);
        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }
}
