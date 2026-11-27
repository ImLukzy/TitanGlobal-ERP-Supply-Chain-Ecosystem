using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ObtenerProgramacionCursoQuery : IRequest<ProgramacionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerProgramacionCursoQueryHandler : IRequestHandler<ObtenerProgramacionCursoQuery, ProgramacionDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerProgramacionCursoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ProgramacionDetalleDto> Handle(ObtenerProgramacionCursoQuery request, CancellationToken cancellationToken)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, request.Id);
        List<InscripcionCurso> inscripciones = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.Empleado)
            .Include(inscripcion => inscripcion.CertificacionCurso)
            .AsNoTracking()
            .Where(inscripcion => inscripcion.ProgramacionCursoId == request.Id)
            .OrderBy(inscripcion => inscripcion.Id)
            .ToListAsync();
        DateOnly hoy = ValidacionCapacitacion.Hoy();
        ProgramacionDetalleDto detalle = new ProgramacionDetalleDto
        {
            Id = programacion.Id,
            CursoId = programacion.CursoId,
            CursoCodigo = programacion.Curso.Codigo,
            CursoNombre = programacion.Curso.Nombre,
            Horas = programacion.Curso.Horas,
            Anio = programacion.Anio,
            FechaInicio = programacion.FechaInicio,
            FechaFin = programacion.FechaFin,
            Cupo = programacion.Cupo,
            Lugar = programacion.Lugar,
            Estado = programacion.Estado
        };
        foreach (InscripcionCurso inscripcion in inscripciones)
        {
            CertificacionDto? certificacion = null;
            if (inscripcion.CertificacionCurso is not null)
            {
                certificacion = new CertificacionDto
                {
                    Id = inscripcion.CertificacionCurso.Id,
                    Codigo = inscripcion.CertificacionCurso.Codigo,
                    FechaEmision = inscripcion.CertificacionCurso.FechaEmision,
                    FechaVencimiento = inscripcion.CertificacionCurso.FechaVencimiento,
                    Vencida = OperacionesCapacitaciones.EstaVencida(inscripcion.CertificacionCurso.FechaVencimiento, hoy)
                };
            }

            detalle.Inscripciones.Add(new InscripcionDto
            {
                Id = inscripcion.Id,
                EmpleadoId = inscripcion.EmpleadoId,
                EmpleadoLegajo = inscripcion.Empleado.Legajo,
                EmpleadoNombre = ValidacionCapacitacion.NombreEmpleado(inscripcion.Empleado),
                Estado = inscripcion.Estado,
                Nota = inscripcion.Nota,
                FechaInscripcion = inscripcion.FechaInscripcion,
                Certificacion = certificacion
            });
        }

        detalle.Inscritos = inscripciones.Count(inscripcion => inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado);
        detalle.CupoLibre = programacion.Cupo - detalle.Inscritos;
        return detalle;
    }
}
