using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

public class ListarEntregasQuery : IRequest<PaginaEntregasEppDto>
{
    public int? EmpleadoId { get; set; }

    public int? EquipoId { get; set; }

    public bool? Vencidas { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarEntregasQueryHandler : IRequestHandler<ListarEntregasQuery, PaginaEntregasEppDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarEntregasQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEntregasEppDto> Handle(ListarEntregasQuery request, CancellationToken cancellationToken)
    {
        ValidacionSst.ValidarPagina(request.Pagina, request.TamanoPagina);
        DateOnly hoy = ValidacionSst.Hoy();
        IQueryable<EntregaEpp> consulta = _unidad.Repository<EntregaEpp>().AsQueryable();
        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(entrega => entrega.EmpleadoId == idEmpleado);
        }

        if (request.EquipoId is not null)
        {
            int idEquipo = request.EquipoId.Value;
            consulta = consulta.Where(entrega => entrega.EquipoProteccionId == idEquipo);
        }

        if (request.Vencidas is not null)
        {
            if (request.Vencidas.Value)
            {
                consulta = consulta.Where(entrega => entrega.FechaVencimiento < hoy);
            }
            else
            {
                consulta = consulta.Where(entrega => entrega.FechaVencimiento >= hoy);
            }
        }

        int total = await consulta.CountAsync();
        List<EntregaEpp> entregas = await consulta
            .Include(entrega => entrega.Empleado)
            .Include(entrega => entrega.EquipoProteccion)
            .OrderByDescending(entrega => entrega.FechaEntrega)
            .ThenByDescending(entrega => entrega.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<EntregaEppDto> items = new List<EntregaEppDto>();
        foreach (EntregaEpp entrega in entregas)
        {
            items.Add(ConvertirEntrega(entrega, hoy));
        }

        return new PaginaEntregasEppDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }

    private static EntregaEppDto ConvertirEntrega(EntregaEpp entrega, DateOnly hoy)
    {
        return new EntregaEppDto
        {
            Id = entrega.Id,
            EquipoId = entrega.EquipoProteccionId,
            EquipoCodigo = entrega.EquipoProteccion.Codigo,
            EquipoNombre = entrega.EquipoProteccion.Nombre,
            EmpleadoId = entrega.EmpleadoId,
            Empleado = ValidacionSst.NombreEmpleado(entrega.Empleado),
            FechaEntrega = entrega.FechaEntrega,
            Cantidad = entrega.Cantidad,
            FechaVencimiento = entrega.FechaVencimiento,
            Vencida = entrega.FechaVencimiento < hoy
        };
    }
}
