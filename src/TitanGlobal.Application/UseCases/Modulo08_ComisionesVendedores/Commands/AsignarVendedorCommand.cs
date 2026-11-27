using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class AsignarVendedorCommand : IRequest<OrdenVendedorDto>
{
    public int OrdenCompraId { get; set; }

    public int VendedorId { get; set; }
}

public class AsignarVendedorCommandHandler : IRequestHandler<AsignarVendedorCommand, OrdenVendedorDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AsignarVendedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenVendedorDto> Handle(AsignarVendedorCommand request, CancellationToken cancellationToken)
    {
        OrdenCompra? orden = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.OrdenCompraId);
        if (orden is null)
        {
            throw new ReglaNegocioException(404, $"La orden de compra {request.OrdenCompraId} no existe.");
        }

        Vendedor vendedor = await ObtenerVendedorParaAsignarAsync(request.VendedorId);
        await OperacionesComisionesVendedores.ValidarMesNoLiquidadoAsync(_unitOfWork, orden.Fecha);
        AsignacionVenta? asignacion = await _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.OrdenCompraId == request.OrdenCompraId);
        if (asignacion is null)
        {
            asignacion = new AsignacionVenta
            {
                OrdenCompraId = request.OrdenCompraId,
                VendedorId = vendedor.Id,
                FechaAsignacion = FechasUtc.Normalizar(DateTime.UtcNow)
            };
            await _unitOfWork.Repository<AsignacionVenta>().AddAsync(asignacion);
        }
        else
        {
            asignacion.VendedorId = vendedor.Id;
            asignacion.FechaAsignacion = FechasUtc.Normalizar(DateTime.UtcNow);
            _unitOfWork.Repository<AsignacionVenta>().Update(asignacion);
        }

        await _unitOfWork.CompleteAsync();
        return await ObtenerOrdenAsync(request.OrdenCompraId);
    }

    private async Task<Vendedor> ObtenerVendedorParaAsignarAsync(int id)
    {
        Vendedor? vendedor = await _unitOfWork.Repository<Vendedor>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (vendedor is null)
        {
            throw new ReglaNegocioException(400, $"El vendedor {id} no existe.");
        }

        if (!vendedor.Activo)
        {
            throw new ReglaNegocioException(400, $"El vendedor {vendedor.Codigo} está inactivo.");
        }

        return vendedor;
    }

    private async Task<OrdenVendedorDto> ObtenerOrdenAsync(int ordenCompraId)
    {
        OrdenVendedorDto? orden = await _unitOfWork.Repository<OrdenCompra>().AsQueryable()
            .Where(entidad => entidad.Id == ordenCompraId)
            .Select(entidad => new OrdenVendedorDto
            {
                OrdenCompraId = entidad.Id,
                NumeroOrden = entidad.NumeroOrden,
                Fecha = entidad.Fecha,
                ClienteNombre = entidad.Cliente.Nombre,
                Total = entidad.Total,
                VendedorId = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == entidad.Id)
                    .Select(asignacion => (int?)asignacion.VendedorId)
                    .FirstOrDefault(),
                VendedorCodigo = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == entidad.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Codigo)
                        .FirstOrDefault())
                    .FirstOrDefault(),
                VendedorNombre = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Where(asignacion => asignacion.OrdenCompraId == entidad.Id)
                    .Select(asignacion => _unitOfWork.Repository<Vendedor>().AsQueryable()
                        .Where(vendedor => vendedor.Id == asignacion.VendedorId)
                        .Select(vendedor => vendedor.Nombre)
                        .FirstOrDefault())
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw new ReglaNegocioException(404, $"La orden de compra {ordenCompraId} no existe.");
        }

        return orden;
    }
}
