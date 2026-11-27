using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Commands;

public class CrearTurnoCommand : IRequest<TurnoIdDto>
{
    public required GuardarTurnoRequestDto Solicitud { get; set; }
}

public class CrearTurnoCommandHandler : IRequestHandler<CrearTurnoCommand, TurnoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearTurnoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<TurnoIdDto> Handle(CrearTurnoCommand request, CancellationToken cancellationToken)
    {
        Turno turno = ValidacionTurno.ValidarTurno(request.Solicitud);
        string codigo = turno.Codigo;
        bool existe = await _unidad.Repository<Turno>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<Turno>().AddAsync(turno);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionTurno.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionTurno.CodigoDuplicado(codigo);
        }

        return new TurnoIdDto { Id = turno.Id };
    }
}
