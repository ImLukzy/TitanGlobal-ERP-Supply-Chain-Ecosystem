using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class InscribirProgramacionCursoCommand : IRequest<InscripcionIdDto>
{
    public int Id { get; set; }

    public required InscribirEmpleadoRequestDto Solicitud { get; set; }
}

public class InscribirProgramacionCursoCommandHandler : IRequestHandler<InscribirProgramacionCursoCommand, InscripcionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public InscribirProgramacionCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<InscripcionIdDto> Handle(InscribirProgramacionCursoCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        int empleadoId = request.Solicitud.EmpleadoId ?? 0;
        Empleado empleado = await ValidacionCapacitacion.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionCapacitacion.EmpleadoActivo)
        {
            throw ValidacionCapacitacion.EmpleadoCesado(empleadoId);
        }

        InscripcionCurso? existente = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .SingleOrDefaultAsync(inscripcion => inscripcion.ProgramacionCursoId == request.Id && inscripcion.EmpleadoId == empleadoId);
        if (existente is not null && existente.Estado != ValidacionCapacitacion.InscripcionRetirado)
        {
            throw ValidacionCapacitacion.InscripcionDuplicada(empleadoId);
        }

        int inscritos = await ValidacionCapacitacion.ContarInscritosAsync(_unidad, request.Id);
        if (inscritos >= programacion.Cupo)
        {
            throw ValidacionCapacitacion.SinCupo(request.Id);
        }

        InscripcionCurso inscripcionNueva = existente ?? new InscripcionCurso
        {
            ProgramacionCursoId = request.Id,
            EmpleadoId = empleadoId
        };
        inscripcionNueva.Estado = ValidacionCapacitacion.InscripcionInscrito;
        inscripcionNueva.Nota = null;
        inscripcionNueva.FechaInscripcion = ValidacionCapacitacion.Hoy();
        try
        {
            if (existente is null)
            {
                await _unidad.Repository<InscripcionCurso>().AddAsync(inscripcionNueva);
            }

            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsInscripcionDuplicada(excepcion))
        {
            throw ValidacionCapacitacion.InscripcionDuplicada(empleadoId);
        }

        return new InscripcionIdDto { Id = inscripcionNueva.Id };
    }
}
