using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ListarCursosQuery : IRequest<PaginaCursosDto>
{
    public string? Busqueda { get; set; }

    public string? Tipo { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarCursosQueryHandler : IRequestHandler<ListarCursosQuery, PaginaCursosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarCursosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaCursosDto> Handle(ListarCursosQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        ValidacionCapacitacion.ValidarPagina(request.Pagina, request.TamanoPagina);
        ValidacionCapacitacion.ValidarTipoFiltro(tipoNormalizado);
        IQueryable<Curso> consulta = _unidad.Repository<Curso>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(curso => curso.Codigo.ToLower().Contains(termino)
                || curso.Nombre.ToLower().Contains(termino));
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(curso => curso.Tipo == tipoNormalizado);
        }

        if (request.Activo is not null)
        {
            bool estaActivo = request.Activo.Value;
            consulta = consulta.Where(curso => curso.Activo == estaActivo);
        }

        int total = await consulta.CountAsync();
        List<Curso> cursos = await consulta
            .OrderBy(curso => curso.Codigo)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<CursoListadoDto> items = new List<CursoListadoDto>();
        foreach (Curso curso in cursos)
        {
            items.Add(OperacionesCursos.Convertir(curso));
        }

        return new PaginaCursosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
