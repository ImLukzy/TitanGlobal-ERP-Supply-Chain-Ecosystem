using MediatR;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class RetirarProgramacionCursoCommand : IRequest<InscripcionIdDto>
{
    public int Id { get; set; }

    public int InscripcionId { get; set; }
}

public class RetirarProgramacionCursoCommandHandler : IRequestHandler<RetirarProgramacionCursoCommand, InscripcionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RetirarProgramacionCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<InscripcionIdDto> Handle(RetirarProgramacionCursoCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        InscripcionCurso inscripcion = await OperacionesProgramacionesCurso.ObtenerInscripcionAsync(_unidad, request.Id, request.InscripcionId);
        bool yaIniciada = ValidacionCapacitacion.Hoy() >= programacion.FechaInicio;
        if (inscripcion.Estado != ValidacionCapacitacion.InscripcionInscrito || yaIniciada)
        {
            throw ValidacionCapacitacion.Error409("Solo se puede retirar una inscripción antes del inicio del curso.");
        }

        inscripcion.Estado = ValidacionCapacitacion.InscripcionRetirado;
        await _unidad.CompleteAsync();
        return new InscripcionIdDto { Id = inscripcion.Id };
    }
}
