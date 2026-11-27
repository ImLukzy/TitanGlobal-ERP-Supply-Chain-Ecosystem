using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Queries;

public class ListarPaisesProveedorQuery : IRequest<List<string>>
{
}

public class ListarPaisesProveedorQueryHandler : IRequestHandler<ListarPaisesProveedorQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarPaisesProveedorQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarPaisesProveedorQuery request, CancellationToken cancellationToken)
    {
        List<string> paises = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Select(proveedor => proveedor.Pais)
            .Distinct()
            .OrderBy(pais => pais)
            .ToListAsync();
        // PATRÓN LINQ 11: valores únicos ordenados.

        return paises;
    }
}
