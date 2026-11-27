using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ResumenCapacitacionesQuery : IRequest<ResumenCapacitacionesDto>
{
    public int? Anio { get; set; }
}

public class ResumenCapacitacionesQueryHandler : IRequestHandler<ResumenCapacitacionesQuery, ResumenCapacitacionesDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenCapacitacionesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenCapacitacionesDto> Handle(ResumenCapacitacionesQuery request, CancellationToken cancellationToken)
    {
        int anioConsulta = ValidacionCapacitacion.ResolverAnio(request.Anio);
        DateOnly hoy = ValidacionCapacitacion.Hoy();
        int cursosActivos = await _unidad.Repository<Curso>().AsQueryable().CountAsync(curso => curso.Activo);
        IQueryable<ProgramacionCurso> programaciones = _unidad.Repository<ProgramacionCurso>().AsQueryable()
            .Where(programacion => programacion.Anio == anioConsulta);
        int programadas = await programaciones
            .CountAsync(programacion => programacion.Estado == ValidacionCapacitacion.ProgramacionProgramada);
        int realizadas = await programaciones
            .CountAsync(programacion => programacion.Estado == ValidacionCapacitacion.ProgramacionRealizada);
        int inscritos = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .CountAsync(inscripcion => inscripcion.ProgramacionCurso.Anio == anioConsulta
                && inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado);
        List<InscripcionCurso> aprobadas = await OperacionesCapacitaciones.ConsultarAprobadasAsync(_unidad, anioConsulta, null);
        int vencidas = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .CountAsync(certificacion => certificacion.FechaVencimiento != null && certificacion.FechaVencimiento < hoy);
        return new ResumenCapacitacionesDto
        {
            Anio = anioConsulta,
            CursosActivos = cursosActivos,
            Programadas = programadas,
            Realizadas = realizadas,
            Inscritos = inscritos,
            HorasTotales = aprobadas.Sum(inscripcion => inscripcion.ProgramacionCurso.Curso.Horas),
            CertificacionesVencidas = vencidas
        };
    }
}
