using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Queries;

public class ListarListasPreciosQuery : IRequest<PaginaListasDto>
{
    public string? Busqueda { get; set; }

    public int? SegmentoId { get; set; }

    public bool? Vigentes { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarListasPreciosQueryHandler : IRequestHandler<ListarListasPreciosQuery, PaginaListasDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarListasPreciosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaListasDto> Handle(ListarListasPreciosQuery request, CancellationToken cancellationToken)
    {
        ValidarPaginacion(request.Pagina, request.TamanoPagina);
        IQueryable<ListaPrecios> consulta = _unitOfWork.Repository<ListaPrecios>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(lista => lista.Codigo.ToLower().Contains(termino)
                || lista.Nombre.ToLower().Contains(termino));
        }

        if (request.SegmentoId.HasValue)
        {
            if (request.SegmentoId.Value == 0)
            {
                consulta = consulta.Where(lista => lista.SegmentoClienteId == null);
            }
            else
            {
                consulta = consulta.Where(lista => lista.SegmentoClienteId == request.SegmentoId.Value);
            }
        }

        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        if (request.Vigentes == true)
        {
            consulta = consulta.Where(lista => lista.Activa && lista.FechaInicio <= hoy
                && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy));
        }

        int total = await consulta.CountAsync();
        long desplazamiento = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = desplazamiento > int.MaxValue ? int.MaxValue : (int)desplazamiento;
        List<ListaListadoDto> items = await consulta.OrderBy(lista => lista.Prioridad).ThenBy(lista => lista.Id)
            .Skip(saltar).Take(request.TamanoPagina)
            .Select(lista => new ListaListadoDto
            {
                Id = lista.Id,
                Codigo = lista.Codigo,
                Nombre = lista.Nombre,
                SegmentoId = lista.SegmentoClienteId,
                SegmentoCodigo = lista.SegmentoCliente == null ? null : lista.SegmentoCliente.Codigo,
                Prioridad = lista.Prioridad,
                FechaInicio = lista.FechaInicio,
                FechaFin = lista.FechaFin,
                Activa = lista.Activa,
                Vigente = lista.Activa && lista.FechaInicio <= hoy && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy),
                Productos = lista.Items.Count
            }).ToListAsync();
        return new PaginaListasDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }

    private static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw OperacionesListasPrecios.Error(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw OperacionesListasPrecios.Error(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}
