using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

public class ListarEvaluacionesProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarEvaluacionesProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaEvaluacionesDto> ExecuteAsync(
        int? proveedorId,
        string? periodo,
        string? calificacion,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        string? periodoNormalizado = string.IsNullOrWhiteSpace(periodo)
            ? null
            : ValidacionEvaluacionProveedor.NormalizarPeriodo(periodo);
        string? calificacionNormalizada = ValidacionEvaluacionProveedor.NormalizarFiltroCalificacion(calificacion);

        IQueryable<EvaluacionProveedor> consulta = _unitOfWork.Repository<EvaluacionProveedor>().AsQueryable();
        if (proveedorId.HasValue)
        {
            consulta = consulta.Where(evaluacion => evaluacion.ProveedorId == proveedorId.Value);
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
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<EvaluacionListadoDto> items = await consulta
            .OrderByDescending(evaluacion => evaluacion.Periodo)
            .ThenBy(evaluacion => evaluacion.ProveedorId)
            .ThenBy(evaluacion => evaluacion.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
