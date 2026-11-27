using MediatR;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class ActivarEquipoProteccionCommand : IRequest<EquipoProteccionIdDto>
{
    public int Id { get; set; }
}

public class ActivarEquipoProteccionCommandHandler : IRequestHandler<ActivarEquipoProteccionCommand, EquipoProteccionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActivarEquipoProteccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EquipoProteccionIdDto> Handle(ActivarEquipoProteccionCommand request, CancellationToken cancellationToken)
    {
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, request.Id);
        if (equipo.Activo)
        {
            throw ValidacionSst.EquipoYaEnEstado(equipo.Codigo, true);
        }

        equipo.Activo = true;
        await _unidad.CompleteAsync();
        return new EquipoProteccionIdDto { Id = equipo.Id };
    }
}
