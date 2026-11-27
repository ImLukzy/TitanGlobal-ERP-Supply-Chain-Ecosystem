using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ObtenerEmpleadoQuery : IRequest<CapacitacionEmpleadoDto>
{
    public int EmpleadoId { get; set; }

    public int? Anio { get; set; }
}

public class ObtenerEmpleadoQueryHandler : IRequestHandler<ObtenerEmpleadoQuery, CapacitacionEmpleadoDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerEmpleadoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CapacitacionEmpleadoDto> Handle(ObtenerEmpleadoQuery request, CancellationToken cancellationToken)
    {
        Empleado empleado = await ValidacionCapacitacion.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId);
        int anioConsulta = ValidacionCapacitacion.ResolverAnio(request.Anio);
        DateOnly inicioAnio = new DateOnly(anioConsulta, 1, 1);
        DateOnly finAnio = new DateOnly(anioConsulta, 12, 31);
        List<InscripcionCurso> delAnio = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.ProgramacionCurso)
            .ThenInclude(programacion => programacion.Curso)
            .AsNoTracking()
            .Where(inscripcion => inscripcion.EmpleadoId == request.EmpleadoId
                && inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado
                && inscripcion.ProgramacionCurso.FechaFin >= inicioAnio
                && inscripcion.ProgramacionCurso.FechaFin <= finAnio)
            .OrderBy(inscripcion => inscripcion.ProgramacionCurso.FechaInicio)
            .ThenBy(inscripcion => inscripcion.Id)
            .ToListAsync();
        List<CertificacionCurso> certificaciones = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .Include(certificacion => certificacion.InscripcionCurso)
            .ThenInclude(inscripcion => inscripcion.ProgramacionCurso)
            .ThenInclude(programacion => programacion.Curso)
            .AsNoTracking()
            .Where(certificacion => certificacion.InscripcionCurso.EmpleadoId == request.EmpleadoId)
            .OrderBy(certificacion => certificacion.FechaEmision)
            .ThenBy(certificacion => certificacion.Id)
            .ToListAsync();
        DateOnly hoy = ValidacionCapacitacion.Hoy();
        CapacitacionEmpleadoDto resultado = new CapacitacionEmpleadoDto
        {
            EmpleadoId = empleado.Id,
            EmpleadoLegajo = empleado.Legajo,
            EmpleadoNombre = ValidacionCapacitacion.NombreEmpleado(empleado),
            Anio = anioConsulta,
            Horas = delAnio
                .Where(inscripcion => inscripcion.Estado == ValidacionCapacitacion.InscripcionAprobado)
                .Sum(inscripcion => inscripcion.ProgramacionCurso.Curso.Horas)
        };
        foreach (InscripcionCurso inscripcion in delAnio)
        {
            resultado.Cursos.Add(new CursoEmpleadoDto
            {
                InscripcionId = inscripcion.Id,
                ProgramacionId = inscripcion.ProgramacionCursoId,
                CursoCodigo = inscripcion.ProgramacionCurso.Curso.Codigo,
                CursoNombre = inscripcion.ProgramacionCurso.Curso.Nombre,
                Horas = inscripcion.ProgramacionCurso.Curso.Horas,
                FechaInicio = inscripcion.ProgramacionCurso.FechaInicio,
                FechaFin = inscripcion.ProgramacionCurso.FechaFin,
                Estado = inscripcion.Estado,
                Nota = inscripcion.Nota
            });
        }

        foreach (CertificacionCurso certificacion in certificaciones)
        {
            resultado.Certificaciones.Add(new CertificacionEmpleadoDto
            {
                Id = certificacion.Id,
                InscripcionId = certificacion.InscripcionCursoId,
                CursoCodigo = certificacion.InscripcionCurso.ProgramacionCurso.Curso.Codigo,
                CursoNombre = certificacion.InscripcionCurso.ProgramacionCurso.Curso.Nombre,
                Codigo = certificacion.Codigo,
                FechaEmision = certificacion.FechaEmision,
                FechaVencimiento = certificacion.FechaVencimiento,
                Vencida = OperacionesCapacitaciones.EstaVencida(certificacion.FechaVencimiento, hoy)
            });
        }

        return resultado;
    }
}
