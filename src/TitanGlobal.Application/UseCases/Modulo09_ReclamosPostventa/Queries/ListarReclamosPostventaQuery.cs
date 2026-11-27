using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo09_ReclamosPostventa.Queries;

public class ListarReclamosPostventaQuery : IRequest<PaginaReclamosDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public string? Tipo { get; set; }

    public string? Prioridad { get; set; }

    public int? ClienteId { get; set; }

    public bool? Vencidos { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarReclamosPostventaQueryHandler : IRequestHandler<ListarReclamosPostventaQuery, PaginaReclamosDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarReclamosPostventaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaReclamosDto> Handle(ListarReclamosPostventaQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        string? estadoNormalizado = ValidacionReclamo.NormalizarEstadoFiltro(request.Estado);
        string? tipoNormalizado = ValidacionReclamo.NormalizarTipoFiltro(request.Tipo);
        string? prioridadNormalizada = ValidacionReclamo.NormalizarPrioridadFiltro(request.Prioridad);
        DateTime ahora = FechasUtc.Normalizar(DateTime.UtcNow);
        IQueryable<ReclamoPostventa> consulta = _unitOfWork.Repository<ReclamoPostventa>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Busqueda))
        {
            string termino = request.Busqueda.Trim().ToLowerInvariant();
            consulta = consulta.Where(reclamo => reclamo.Numero.ToLower().Contains(termino)
                || _unitOfWork.Repository<OrdenCompra>().AsQueryable()
                    .Any(orden => orden.Id == reclamo.OrdenCompraId
                        && orden.NumeroOrden.ToLower().Contains(termino))
                || _unitOfWork.Repository<Cliente>().AsQueryable()
                    .Any(cliente => cliente.Id == reclamo.ClienteId
                        && cliente.Nombre.ToLower().Contains(termino)));
        }

        if (estadoNormalizado is not null)
        {
            consulta = consulta.Where(reclamo => reclamo.Estado == estadoNormalizado);
        }

        if (tipoNormalizado is not null)
        {
            consulta = consulta.Where(reclamo => reclamo.Tipo == tipoNormalizado);
        }

        if (prioridadNormalizada is not null)
        {
            consulta = consulta.Where(reclamo => reclamo.Prioridad == prioridadNormalizada);
        }

        if (request.ClienteId.HasValue)
        {
            consulta = consulta.Where(reclamo => reclamo.ClienteId == request.ClienteId.Value);
        }

        if (request.Vencidos == true)
        {
            consulta = consulta.Where(reclamo =>
                (reclamo.Estado == "Abierto" || reclamo.Estado == "EnProceso")
                && reclamo.FechaLimite < ahora);
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<ReclamoPostventa> reclamos = await consulta
            .OrderBy(reclamo => reclamo.Prioridad == "Alta" ? 1 : reclamo.Prioridad == "Media" ? 2 : 3)
            .ThenBy(reclamo => reclamo.FechaLimite)
            .ThenBy(reclamo => reclamo.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<ReclamoListadoDto> items = await OperacionesReclamosPostventa.ProyectarListadoAsync(_unitOfWork, reclamos, ahora);
        return new PaginaReclamosDto
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
