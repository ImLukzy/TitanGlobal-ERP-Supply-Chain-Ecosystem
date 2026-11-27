using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Queries;

public class ResumenProveedoresQuery : IRequest<ResumenProveedoresDto>
{
}

public class ResumenProveedoresQueryHandler : IRequestHandler<ResumenProveedoresQuery, ResumenProveedoresDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenProveedoresQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenProveedoresDto> Handle(ResumenProveedoresQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Proveedor> proveedores = _unitOfWork.Repository<Proveedor>().AsQueryable();
        int totalProveedores = await proveedores.CountAsync();
        int activos = await proveedores.CountAsync(proveedor => proveedor.Activo);
        int inactivos = totalProveedores - activos;
        int sinEmail = await proveedores.CountAsync(proveedor => string.IsNullOrEmpty(proveedor.Email));
        List<PaisProveedoresDto> porPais = await proveedores
            .GroupBy(proveedor => proveedor.Pais)
            .Select(grupo => new PaisProveedoresDto
            {
                Pais = grupo.Key,
                TotalProveedores = grupo.Count(),
                Activos = grupo.Count(proveedor => proveedor.Activo)
            })
            .OrderByDescending(pais => pais.TotalProveedores)
            .ThenBy(pais => pais.Pais)
            .ToListAsync();
        // PATRÓN LINQ 9: agrupación y orden por conteo.

        return new ResumenProveedoresDto
        {
            TotalProveedores = totalProveedores,
            Activos = activos,
            Inactivos = inactivos,
            SinEmail = sinEmail,
            PorPais = porPais
        };
    }
}
