using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class CrearCursoCommand : IRequest<CursoIdDto>
{
    public required GuardarCursoRequestDto Solicitud { get; set; }
}

public class CrearCursoCommandHandler : IRequestHandler<CrearCursoCommand, CursoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public CrearCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CursoIdDto> Handle(CrearCursoCommand request, CancellationToken cancellationToken)
    {
        Curso curso = ValidacionCapacitacion.ValidarCurso(request.Solicitud);
        string codigo = curso.Codigo;
        bool existe = await _unidad.Repository<Curso>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionCapacitacion.CodigoCursoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<Curso>().AddAsync(curso);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsCodigoCursoDuplicado(excepcion))
        {
            throw ValidacionCapacitacion.CodigoCursoDuplicado(codigo);
        }

        return new CursoIdDto { Id = curso.Id };
    }
}
