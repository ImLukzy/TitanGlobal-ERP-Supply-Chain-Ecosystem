using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes.Queries;

public class ListarCiudadesAlmacenQuery : IRequest<List<string>>
{
}

public class ListarCiudadesAlmacenQueryHandler : IRequestHandler<ListarCiudadesAlmacenQuery, List<string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarCiudadesAlmacenQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> Handle(ListarCiudadesAlmacenQuery request, CancellationToken cancellationToken)
    {
        List<string> ciudades = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .Select(almacen => almacen.Ciudad)
            .Distinct()
            .OrderBy(ciudad => ciudad)
            .ToListAsync();
        // PATRÓN LINQ 11: valores únicos ordenados.

        return ciudades;
    }
}
