using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Queries;

public class ObtenerEvaluacionQuery : IRequest<EvaluacionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerEvaluacionQueryHandler : IRequestHandler<ObtenerEvaluacionQuery, EvaluacionDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerEvaluacionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EvaluacionDetalleDto> Handle(ObtenerEvaluacionQuery request, CancellationToken cancellationToken)
    {
        EvaluacionDetalleDto? detalle = await _unidad.Repository<Evaluacion>().AsQueryable()
            .Where(evaluacion => evaluacion.Id == request.Id)
            .Select(evaluacion => new EvaluacionDetalleDto
            {
                Id = evaluacion.Id,
                CicloId = evaluacion.CicloEvaluacionId,
                CicloCodigo = evaluacion.CicloEvaluacion.Codigo,
                EmpleadoId = evaluacion.EmpleadoId,
                EmpleadoLegajo = evaluacion.Empleado.Legajo,
                EmpleadoNombre = evaluacion.Empleado.Apellidos + ", " + evaluacion.Empleado.Nombres,
                EvaluadorId = evaluacion.EvaluadorId,
                EvaluadorNombre = evaluacion.Evaluador.Apellidos + ", " + evaluacion.Evaluador.Nombres,
                Estado = evaluacion.Estado,
                Comentario = evaluacion.Comentario,
                PuntajeFinal = evaluacion.PuntajeFinal,
                FechaEnvio = evaluacion.FechaEnvio,
                FechaCierre = evaluacion.FechaCierre
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionEvaluacion.EvaluacionInexistente(request.Id);
        }

        detalle.Etiqueta = detalle.PuntajeFinal is null ? null : ValidacionEvaluacion.Etiqueta(detalle.PuntajeFinal.Value);
        List<CompetenciaEvaluacion> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(competencia => competencia.CicloEvaluacionId == detalle.CicloId)
            .OrderBy(competencia => competencia.Id)
            .ToListAsync();
        List<CalificacionEvaluacion> calificaciones = await _unidad.Repository<CalificacionEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(calificacion => calificacion.EvaluacionId == request.Id)
            .ToListAsync();
        detalle.Calificaciones = competencias
            .Select(competencia => new CalificacionEvaluacionDto
            {
                CompetenciaId = competencia.Id,
                Competencia = competencia.Nombre,
                Peso = competencia.Peso,
                Calificacion = calificaciones
                    .Where(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id)
                    .Select(calificacion => (int?)calificacion.Calificacion)
                    .FirstOrDefault()
            })
            .ToList();
        return detalle;
    }
}
