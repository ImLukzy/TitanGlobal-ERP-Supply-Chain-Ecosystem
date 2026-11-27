using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class RegistrarResultadosCommand : IRequest<ProgramacionIdDto>
{
    public int Id { get; set; }

    public required RegistrarResultadosRequestDto Solicitud { get; set; }
}

public class RegistrarResultadosCommandHandler : IRequestHandler<RegistrarResultadosCommand, ProgramacionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public RegistrarResultadosCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // Todo se valida antes de modificar nada: un resultado inválido no deja cambios parciales.
    public async Task<ProgramacionIdDto> Handle(RegistrarResultadosCommand request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        if (programacion.Estado != ValidacionCapacitacion.ProgramacionRealizada)
        {
            throw ValidacionCapacitacion.Error409($"La programación {request.Id} debe estar realizada para registrar resultados.");
        }

        List<InscripcionCurso> inscripciones = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.CertificacionCurso)
            .Where(inscripcion => inscripcion.ProgramacionCursoId == request.Id)
            .ToListAsync();
        List<(InscripcionCurso Inscripcion, ResultadoInscripcionRequestDto Resultado)> cambios =
            new List<(InscripcionCurso Inscripcion, ResultadoInscripcionRequestDto Resultado)>();
        foreach (ResultadoInscripcionRequestDto resultado in request.Solicitud.Resultados ?? new List<ResultadoInscripcionRequestDto>())
        {
            int inscripcionId = resultado.InscripcionId ?? 0;
            InscripcionCurso? inscripcion = inscripciones.FirstOrDefault(entidad => entidad.Id == inscripcionId);
            if (inscripcion is null || inscripcion.Estado == ValidacionCapacitacion.InscripcionRetirado)
            {
                throw ValidacionCapacitacion.InscripcionInexistente(inscripcionId);
            }

            ValidacionCapacitacion.ValidarNota(resultado.Nota);
            if (inscripcion.CertificacionCurso is not null)
            {
                throw ValidacionCapacitacion.CertificacionDuplicada(inscripcionId);
            }

            cambios.Add((inscripcion, resultado));
        }

        foreach ((InscripcionCurso inscripcion, ResultadoInscripcionRequestDto resultado) in cambios)
        {
            inscripcion.Estado = resultado.Aprobado
                ? ValidacionCapacitacion.InscripcionAprobado
                : ValidacionCapacitacion.InscripcionDesaprobado;
            inscripcion.Nota = resultado.Nota;
        }

        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }
}
