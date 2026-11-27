using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo10_DevolucionesComerciales.Queries;

public class ListarDevolucionesComercialesQuery : IRequest<PaginaDevolucionesDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public string? Motivo { get; set; }

    public int? ClienteId { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarDevolucionesComercialesQueryHandler : IRequestHandler<ListarDevolucionesComercialesQuery, PaginaDevolucionesDto>
{
    private readonly IUnitOfWork _unidadTrabajo;

    public ListarDevolucionesComercialesQueryHandler(IUnitOfWork unidadTrabajo)
    {
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<PaginaDevolucionesDto> Handle(ListarDevolucionesComercialesQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        string? estadoNormalizado = ValidacionDevolucion.NormalizarEstadoFiltro(request.Estado);
        string? motivoNormalizado = ValidacionDevolucion.NormalizarMotivoFiltro(request.Motivo);
        IQueryable<DevolucionComercial> consulta =
            _unidadTrabajo.Repository<DevolucionComercial>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(devolucion => devolucion.Numero.ToLower().Contains(termino)
                || _unidadTrabajo.Repository<OrdenCompra>().AsQueryable()
                    .Any(orden => orden.Id == devolucion.OrdenCompraId
                        && orden.NumeroOrden.ToLower().Contains(termino))
                || _unidadTrabajo.Repository<Cliente>().AsQueryable()
                    .Any(cliente => cliente.Id == devolucion.ClienteId
                        && cliente.Nombre.ToLower().Contains(termino)));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(devolucion => devolucion.Estado == estadoNormalizado);
        }

        if (motivoNormalizado is not null)
        {
            consulta = consulta.Where(devolucion => devolucion.Motivo == motivoNormalizado);
        }

        if (request.ClienteId.HasValue)
        {
            consulta = consulta.Where(devolucion => devolucion.ClienteId == request.ClienteId.Value);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<DevolucionComercial> devoluciones = await consulta
            .OrderByDescending(devolucion => devolucion.Fecha)
            .ThenByDescending(devolucion => devolucion.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<DevolucionListadoDto> items = await OperacionesDevolucionesComerciales.ProyectarListadoAsync(_unidadTrabajo, devoluciones);
        return new PaginaDevolucionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}
