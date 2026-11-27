using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional.Commands;

public class ActualizarEquipoProteccionCommand : IRequest<EquipoProteccionIdDto>
{
    public int Id { get; set; }

    public required GuardarEquipoProteccionRequestDto Solicitud { get; set; }
}

public class ActualizarEquipoProteccionCommandHandler : IRequestHandler<ActualizarEquipoProteccionCommand, EquipoProteccionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarEquipoProteccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<EquipoProteccionIdDto> Handle(ActualizarEquipoProteccionCommand request, CancellationToken cancellationToken)
    {
        EquipoProteccion nuevo = ValidacionSst.ValidarEquipo(request.Solicitud);
        EquipoProteccion equipo = await ValidacionSst.ObtenerEquipoAsync(_unidad, request.Id);
        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<EquipoProteccion>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != request.Id);
        if (existe)
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        equipo.Codigo = nuevo.Codigo;
        equipo.Nombre = nuevo.Nombre;
        equipo.VidaUtilDias = nuevo.VidaUtilDias;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionSst.EsCodigoEquipoDuplicado(excepcion))
        {
            throw ValidacionSst.CodigoEquipoDuplicado(codigo);
        }

        return new EquipoProteccionIdDto { Id = equipo.Id };
    }
}
