using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

public class ListarEquiposProteccionQuery : IRequest<PaginaEquiposProteccionDto>
{
    public string? Busqueda { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarEquiposProteccionQueryHandler : IRequestHandler<ListarEquiposProteccionQuery, PaginaEquiposProteccionDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarEquiposProteccionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEquiposProteccionDto> Handle(ListarEquiposProteccionQuery request, CancellationToken cancellationToken)
    {
        ValidacionSst.ValidarPagina(request.Pagina, request.TamanoPagina);
        IQueryable<EquipoProteccion> consulta = _unidad.Repository<EquipoProteccion>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(equipo => equipo.Codigo.ToLower().Contains(termino)
                || equipo.Nombre.ToLower().Contains(termino));
        }

        if (request.Activo is not null)
        {
            bool estaActivo = request.Activo.Value;
            consulta = consulta.Where(equipo => equipo.Activo == estaActivo);
        }

        int total = await consulta.CountAsync();
        List<EquipoProteccion> equipos = await consulta
            .OrderBy(equipo => equipo.Codigo)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<EquipoProteccionListadoDto> items = new List<EquipoProteccionListadoDto>();
        foreach (EquipoProteccion equipo in equipos)
        {
            items.Add(OperacionesEquiposProteccion.Convertir(equipo));
        }

        return new PaginaEquiposProteccionDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
