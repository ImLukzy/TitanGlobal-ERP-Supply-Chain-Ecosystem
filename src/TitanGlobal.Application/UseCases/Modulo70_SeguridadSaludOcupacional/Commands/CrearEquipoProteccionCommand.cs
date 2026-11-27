using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class CrearEquipoProteccionCommand : IRequest<EquipoProteccionIdDto>
{
    public required GuardarEquipoProteccionRequestDto Solicitud { get; set; }
}

public class CrearEquipoProteccionCommandHandler : IRequestHandler<CrearEquipoProteccionCommand, EquipoProteccionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearEquipoProteccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EquipoProteccionIdDto> Handle(CrearEquipoProteccionCommand request, CancellationToken cancellationToken)
    {
        EquipoProteccion equipo = ValidacionSst.ValidarEquipo(request.Solicitud);
        string codigo = equipo.Codigo;
        bool existe = await _unidad.Repository<EquipoProteccion>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<EquipoProteccion>().AddAsync(equipo);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionSst.EsCodigoEquipoDuplicado(excepcion))
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        return new EquipoProteccionIdDto { Id = equipo.Id };
    }
}
