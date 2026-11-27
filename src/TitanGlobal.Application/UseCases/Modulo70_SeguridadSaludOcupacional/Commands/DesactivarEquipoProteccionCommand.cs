using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class DesactivarEquipoProteccionCommand : IRequest<EquipoProteccionIdDto>
{
    public int Id { get; set; }
}

public class DesactivarEquipoProteccionCommandHandler : IRequestHandler<DesactivarEquipoProteccionCommand, EquipoProteccionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public DesactivarEquipoProteccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EquipoProteccionIdDto> Handle(DesactivarEquipoProteccionCommand request, CancellationToken cancellationToken)
    {
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, request.Id);
        if (!equipo.Activo)
        {
            throw ValidacionSst.EquipoYaEnEstado(equipo.Codigo, false);
        }

        equipo.Activo = false;
        await _unidad.CompleteAsync();
        return new EquipoProteccionIdDto { Id = equipo.Id };
    }
}
