using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Queries;

public class ObtenerVendedorQuery : IRequest<VendedorDetalleDto>
{
    public int Id { get; set; }
}

public class ObtenerVendedorQueryHandler : IRequestHandler<ObtenerVendedorQuery, VendedorDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerVendedorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VendedorDetalleDto> Handle(ObtenerVendedorQuery request, CancellationToken cancellationToken)
    {
        Vendedor vendedor = await OperacionesComisionesVendedores.ObtenerVendedorEntidadAsync(_unitOfWork, request.Id);
        List<int> ordenesIds = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .Where(asignacion => asignacion.VendedorId == request.Id)
            .Select(asignacion => asignacion.OrdenCompraId)
            .ToListAsync();
        decimal ventas = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(orden => ordenesIds.Contains(orden.Id))
            .SumAsync(orden => (decimal?)orden.Total) ?? 0m;
        int cantidad = ordenesIds.Count;
        return new VendedorDetalleDto
        {
            Id = vendedor.Id,
            Codigo = vendedor.Codigo,
            Nombre = vendedor.Nombre,
            PorcentajeComision = vendedor.PorcentajeComision,
            Activo = vendedor.Activo,
            Email = vendedor.Email,
            OrdenesAsignadas = cantidad,
            VentasTotales = ventas
        };
    }
}
