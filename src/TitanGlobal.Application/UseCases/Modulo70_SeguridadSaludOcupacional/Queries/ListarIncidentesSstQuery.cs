using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

public class ListarIncidentesSstQuery : IRequest<PaginaIncidentesSstDto>
{
    public string? Busqueda { get; set; }

    public string? Tipo { get; set; }

    public string? Severidad { get; set; }

    public string? Estado { get; set; }

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarIncidentesSstQueryHandler : IRequestHandler<ListarIncidentesSstQuery, PaginaIncidentesSstDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarIncidentesSstQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaIncidentesSstDto> Handle(ListarIncidentesSstQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        string severidadNormalizada = request.Severidad?.Trim() ?? string.Empty;
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionSst.ValidarPagina(request.Pagina, request.TamanoPagina);
        ValidacionSst.ValidarFiltrosIncidente(tipoNormalizado, severidadNormalizada, estadoNormalizado, request.Desde, request.Hasta);
        IQueryable<IncidenteSst> consulta = _unidad.Repository<IncidenteSst>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(incidente => incidente.Numero.ToLower().Contains(termino)
                || incidente.Descripcion.ToLower().Contains(termino)
                || (incidente.Lugar != null && incidente.Lugar.ToLower().Contains(termino)));
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(incidente => incidente.Tipo == tipoNormalizado);
        }

        if (severidadNormalizada.Length > 0)
        {
            consulta = consulta.Where(incidente => incidente.Severidad == severidadNormalizada);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(incidente => incidente.Estado == estadoNormalizado);
        }

        if (request.Desde is not null)
        {
            DateOnly inicio = request.Desde.Value;
            consulta = consulta.Where(incidente => incidente.Fecha >= inicio);
        }

        if (request.Hasta is not null)
        {
            DateOnly fin = request.Hasta.Value;
            consulta = consulta.Where(incidente => incidente.Fecha <= fin);
        }

        int total = await consulta.CountAsync();
        List<IncidenteSst> incidentes = await consulta
            .Include(incidente => incidente.Empleado)
            .Include(incidente => incidente.AccionesCorrectivasSst)
            .OrderByDescending(incidente => incidente.Fecha)
            .ThenByDescending(incidente => incidente.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<IncidenteSstListadoDto> items = new List<IncidenteSstListadoDto>();
        foreach (IncidenteSst incidente in incidentes)
        {
            items.Add(ConvertirListado(incidente));
        }

        return new PaginaIncidentesSstDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static IncidenteSstListadoDto ConvertirListado(IncidenteSst incidente)
    {
        return new IncidenteSstListadoDto
        {
            Id = incidente.Id,
            Numero = incidente.Numero,
            EmpleadoId = incidente.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(incidente.Empleado),
            Tipo = incidente.Tipo,
            Severidad = incidente.Severidad,
            Fecha = incidente.Fecha,
            Lugar = incidente.Lugar,
            Descripcion = incidente.Descripcion,
            DiasBaja = incidente.DiasBaja,
            Estado = incidente.Estado,
            FechaCierre = incidente.FechaCierre,
            Acciones = incidente.AccionesCorrectivasSst.Count,
            AccionesPendientes = incidente.AccionesCorrectivasSst.Count(accion => !accion.Completada)
        };
    }
}
