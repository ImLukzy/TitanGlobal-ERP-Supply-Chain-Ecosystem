using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo06_ListasPrecios.Queries;

public class ObtenerListaPreciosQuery : IRequest<ListaDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerListaPreciosQueryHandler : IRequestHandler<ObtenerListaPreciosQuery, ListaDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerListaPreciosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ListaDetalleDto> Handle(ObtenerListaPreciosQuery request, CancellationToken cancellationToken)
    {
        DateTime hoy = FechasUtc.Normalizar(DateTime.UtcNow).Date;
        ListaPrecios? lista = await _unitOfWork.Repository<ListaPrecios>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (lista is null)
        {
            throw OperacionesListasPrecios.Error(CodigosEstado.Status404NotFound, $"La lista de precios {request.Id} no existe.");
        }

        string? segmentoCodigo = await _unitOfWork.Repository<SegmentoCliente>().AsQueryable()
            .Where(segmento => segmento.Id == lista.SegmentoClienteId).Select(segmento => segmento.Codigo).SingleOrDefaultAsync();
        List<ItemListaDto> items = await _unitOfWork.Repository<ItemListaPrecios>().AsQueryable()
            .Where(item => item.ListaPreciosId == request.Id)
            .Join(_unitOfWork.Repository<Producto>().AsQueryable(), item => item.ProductoId, producto => producto.Id,
                (item, producto) => new ItemListaDto
                {
                    ProductoId = producto.Id,
                    ProductoNombre = producto.Nombre,
                    PrecioBase = producto.Precio,
                    Precio = item.Precio,
                    DiferenciaPorcentaje = producto.Precio == 0m ? 0m
                        : decimal.Round((item.Precio - producto.Precio) / producto.Precio * 100m, 2)
                })
            .OrderBy(item => item.ProductoNombre).ToListAsync();
        ListaListadoDto listado = ProyectarListado(lista, hoy, segmentoCodigo, items.Count);
        return new ListaDetalleDto
        {
            Id = listado.Id,
            Codigo = listado.Codigo,
            Nombre = listado.Nombre,
            SegmentoId = listado.SegmentoId,
            SegmentoCodigo = listado.SegmentoCodigo,
            Prioridad = listado.Prioridad,
            FechaInicio = listado.FechaInicio,
            FechaFin = listado.FechaFin,
            Activa = listado.Activa,
            Vigente = listado.Vigente,
            Productos = listado.Productos,
            Items = items
        };
    }

    private static ListaListadoDto ProyectarListado(ListaPrecios lista, DateTime hoy, string? segmentoCodigo = null, int? productos = null)
    {
        return new ListaListadoDto
        {
            Id = lista.Id,
            Codigo = lista.Codigo,
            Nombre = lista.Nombre,
            SegmentoId = lista.SegmentoClienteId,
            SegmentoCodigo = segmentoCodigo,
            Prioridad = lista.Prioridad,
            FechaInicio = lista.FechaInicio,
            FechaFin = lista.FechaFin,
            Activa = lista.Activa,
            Vigente = lista.Activa && lista.FechaInicio <= hoy && (!lista.FechaFin.HasValue || lista.FechaFin.Value >= hoy),
            Productos = productos ?? lista.Items.Count
        };
    }
}
