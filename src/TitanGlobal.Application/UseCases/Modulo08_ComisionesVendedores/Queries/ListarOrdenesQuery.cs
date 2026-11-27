using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Queries;

public class ListarOrdenesQuery : IRequest<PaginaOrdenesVendedorDto>
{
    public int? VendedorId { get; set; }

    public bool? SinAsignar { get; set; }

    public int? Anio { get; set; }

    public int? Mes { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarOrdenesQueryHandler : IRequestHandler<ListarOrdenesQuery, PaginaOrdenesVendedorDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarOrdenesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaOrdenesVendedorDto> Handle(ListarOrdenesQuery request, CancellationToken cancellationToken)
    {
        ValidacionComisiones.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        if (request.Mes.HasValue && (request.Mes.Value < 1 || request.Mes.Value > 12))
        {
            throw new ReglaNegocioException(400, "El mes no es válido.");
        }

        IQueryable<OrdenCompra> ordenes = _unitOfWork.Repository<OrdenCompra>().AsQueryable();
        if (request.Anio.HasValue)
        {
            ordenes = ordenes.Where(orden => orden.Fecha.Year == request.Anio.Value);
        }

        if (request.Mes.HasValue)
        {
            ordenes = ordenes.Where(orden => orden.Fecha.Month == request.Mes.Value);
        }

        if (request.VendedorId.HasValue)
        {
            ordenes = ordenes.Where(orden => _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                .Any(asignacion => asignacion.OrdenCompraId == orden.Id && asignacion.VendedorId == request.VendedorId.Value));
        }

        if (request.SinAsignar == true)
        {
            ordenes = ordenes.Where(orden => !_unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                .Any(asignacion => asignacion.OrdenCompraId == orden.Id));
        }

        int total = await ordenes.CountAsync();
        List<OrdenVendedorDto> items = await ordenes
            .OrderByDescending(orden => orden.Fecha)
            .ThenByDescending(orden => orden.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(orden => new OrdenVendedorDto
            {
                OrdenCompraId = orden.Id,
                NumeroOrden = orden.NumeroOrden,
                Fecha = orden.Fecha,
                ClienteNombre = orden.Cliente.Nombre,
                Total = orden.Total,
                VendedorId = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == orden.Id)
                    .Select(asignacion => (int?)asignacion.VendedorId)
                    .FirstOrDefault(),
                VendedorCodigo = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == orden.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Codigo)
                        .FirstOrDefault())
                    .FirstOrDefault(),
                VendedorNombre = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == orden.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Nombre)
                        .FirstOrDefault())
                    .FirstOrDefault()
            })
            .ToListAsync();
        return new PaginaOrdenesVendedorDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
