using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Queries;

public class ListarEvaluacionesQuery : IRequest<PaginaEvaluacionesDto>
{
    public int? CicloId { get; set; }

    public int? EmpleadoId { get; set; }

    public string? Estado { get; set; }

    public string? Busqueda { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarEvaluacionesQueryHandler : IRequestHandler<ListarEvaluacionesQuery, PaginaEvaluacionesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarEvaluacionesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEvaluacionesDto> Handle(ListarEvaluacionesQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionEvaluacion.ValidarPagina(request.Pagina, request.TamanoPagina);
        ValidacionEvaluacion.ValidarEstadoEvaluacion(estadoNormalizado);
        if (request.CicloId is not null)
        {
            bool existeCiclo = await _unidad.Repository<CicloEvaluacion>().AsQueryable()
                .AnyAsync(ciclo => ciclo.Id == request.CicloId.Value);
            if (!existeCiclo)
            {
                throw ValidacionEvaluacion.CicloInexistente(request.CicloId.Value);
            }
        }

        if (request.EmpleadoId is not null)
        {
            await ValidacionEvaluacion.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId.Value);
        }

        IQueryable<Evaluacion> consulta = _unidad.Repository<Evaluacion>().AsQueryable();
        if (request.CicloId is not null)
        {
            int idCiclo = request.CicloId.Value;
            consulta = consulta.Where(evaluacion => evaluacion.CicloEvaluacionId == idCiclo);
        }

        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(evaluacion => evaluacion.EmpleadoId == idEmpleado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(evaluacion => evaluacion.Estado == estadoNormalizado);
        }

        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(evaluacion => evaluacion.CicloEvaluacion.Codigo.ToLower().Contains(termino)
                || evaluacion.Empleado.Nombres.ToLower().Contains(termino)
                || evaluacion.Empleado.Apellidos.ToLower().Contains(termino)
                || evaluacion.Empleado.Legajo.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        List<EvaluacionListadoDto> items = await consulta
            .OrderByDescending(evaluacion => evaluacion.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(evaluacion => new EvaluacionListadoDto
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
                PuntajeFinal = evaluacion.PuntajeFinal
            })
            .ToListAsync();
        foreach (EvaluacionListadoDto item in items)
        {
            item.Etiqueta = item.PuntajeFinal is null ? null : ValidacionEvaluacion.Etiqueta(item.PuntajeFinal.Value);
        }

        return new PaginaEvaluacionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
