using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores.Queries;

public class ListarEvaluacionesProveedorQuery : IRequest<PaginaEvaluacionesDto>
{
    public int? ProveedorId { get; set; }

    public string? Periodo { get; set; }

    public string? Calificacion { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarEvaluacionesProveedorQueryHandler : IRequestHandler<ListarEvaluacionesProveedorQuery, PaginaEvaluacionesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarEvaluacionesProveedorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaEvaluacionesDto> Handle(ListarEvaluacionesProveedorQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        string? periodoNormalizado = string.IsNullOrWhiteSpace(request.Periodo)
            ? null
            : ValidacionEvaluacionProveedor.NormalizarPeriodo(request.Periodo);
        string? calificacionNormalizada = ValidacionEvaluacionProveedor.NormalizarFiltroCalificacion(request.Calificacion);

        IQueryable<EvaluacionProveedor> consulta = _unitOfWork.Repository<EvaluacionProveedor>().AsQueryable();
        if (request.ProveedorId.HasValue)
        {
            consulta = consulta.Where(evaluacion => evaluacion.ProveedorId == request.ProveedorId.Value);
        }

        if (periodoNormalizado is not null)
        {
            consulta = consulta.Where(evaluacion => evaluacion.Periodo == periodoNormalizado);
        }

        if (calificacionNormalizada is not null)
        {
            consulta = consulta.Where(evaluacion => evaluacion.Calificacion == calificacionNormalizada);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<EvaluacionListadoDto> items = await consulta
            .OrderByDescending(evaluacion => evaluacion.Periodo)
            .ThenBy(evaluacion => evaluacion.ProveedorId)
            .ThenBy(evaluacion => evaluacion.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
            .Select(evaluacion => new EvaluacionListadoDto
            {
                Id = evaluacion.Id,
                ProveedorId = evaluacion.ProveedorId,
                ProveedorNombre = evaluacion.Proveedor.RazonSocial,
                Periodo = evaluacion.Periodo,
                PuntajeFinal = evaluacion.PuntajeFinal,
                Calificacion = evaluacion.Calificacion
            })
            .ToListAsync();

        return new PaginaEvaluacionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
