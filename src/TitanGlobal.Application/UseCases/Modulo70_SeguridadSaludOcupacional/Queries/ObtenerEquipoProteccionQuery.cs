using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Queries;

public class ObtenerEquipoProteccionQuery : IRequest<EquipoProteccionListadoDto>
{
    public int Id { get; set; }
}

public class ObtenerEquipoProteccionQueryHandler : IRequestHandler<ObtenerEquipoProteccionQuery, EquipoProteccionListadoDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerEquipoProteccionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EquipoProteccionListadoDto> Handle(ObtenerEquipoProteccionQuery request, CancellationToken cancellationToken)
    {
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, request.Id);
        return OperacionesEquiposProteccion.Convertir(equipo);
    }
}
