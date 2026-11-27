using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Queries;

public class ListarVendedoresQuery : IRequest<PaginaVendedoresDto>
{
    public string? Busqueda { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarVendedoresQueryHandler : IRequestHandler<ListarVendedoresQuery, PaginaVendedoresDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarVendedoresQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaVendedoresDto> Handle(ListarVendedoresQuery request, CancellationToken cancellationToken)
    {
        ValidacionComisiones.ValidarPaginacion(request.Pagina, request.TamanoPagina);
        IQueryable<Vendedor> consulta = _unitOfWork.Repository<Vendedor>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(vendedor => vendedor.Codigo.ToLower().Contains(termino)
                || vendedor.Nombre.ToLower().Contains(termino));
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(vendedor => vendedor.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();
        List<VendedorListadoDto> items = await consulta
            .OrderBy(vendedor => vendedor.Codigo)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(vendedor => new VendedorListadoDto
            {
                Id = vendedor.Id,
                Codigo = vendedor.Codigo,
                Nombre = vendedor.Nombre,
                PorcentajeComision = vendedor.PorcentajeComision,
                Activo = vendedor.Activo,
                OrdenesAsignadas = _unitOfWork.Repository<AsignacionVenta>().AsQueryable()
                    .Count(asignacion => asignacion.VendedorId == vendedor.Id)
            })
            .ToListAsync();
        return new PaginaVendedoresDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
