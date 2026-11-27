using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Queries;

public class ObtenerCicloEvaluacionQuery : IRequest<CicloEvaluacionDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerCicloEvaluacionQueryHandler : IRequestHandler<ObtenerCicloEvaluacionQuery, CicloEvaluacionDetalleDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerCicloEvaluacionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CicloEvaluacionDetalleDto> Handle(ObtenerCicloEvaluacionQuery request, CancellationToken cancellationToken)
    {
        CicloEvaluacion ciclo = await OperacionesCiclosEvaluacion.ObtenerSinSeguimientoAsync(_unidad, request.Id);
        List<CompetenciaEvaluacionDto> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .Where(competencia => competencia.CicloEvaluacionId == request.Id)
            .OrderBy(competencia => competencia.Id)
            .Select(competencia => new CompetenciaEvaluacionDto
            {
                Id = competencia.Id,
                Nombre = competencia.Nombre,
                Descripcion = competencia.Descripcion,
                Peso = competencia.Peso
            })
            .ToListAsync();
        List<string> estados = await _unidad.Repository<Evaluacion>().AsQueryable()
            .Where(evaluacion => evaluacion.CicloEvaluacionId == request.Id)
            .Select(evaluacion => evaluacion.Estado)
            .ToListAsync();
        return new CicloEvaluacionDetalleDto
        {
            Id = ciclo.Id,
            Codigo = ciclo.Codigo,
            Nombre = ciclo.Nombre,
            Periodo = ciclo.Periodo,
            FechaInicioPeriodo = ciclo.FechaInicioPeriodo,
            FechaFinPeriodo = ciclo.FechaFinPeriodo,
            FechaApertura = ciclo.FechaApertura,
            FechaCierre = ciclo.FechaCierre,
            Estado = ciclo.Estado,
            SumaPesos = competencias.Sum(competencia => competencia.Peso),
            Competencias = competencias,
            Evaluaciones = new EvaluacionesPorEstadoDto
            {
                Borrador = estados.Count(estado => estado == ValidacionEvaluacion.EvaluacionBorrador),
                Enviada = estados.Count(estado => estado == ValidacionEvaluacion.EvaluacionEnviada),
                Cerrada = estados.Count(estado => estado == ValidacionEvaluacion.EvaluacionCerrada)
            }
        };
    }
}
