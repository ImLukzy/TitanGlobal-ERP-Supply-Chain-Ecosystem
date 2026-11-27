using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class ActualizarCursoCommand : IRequest<CursoIdDto>
{
    public int Id { get; set; }

    public required GuardarCursoRequestDto Solicitud { get; set; }
}

public class ActualizarCursoCommandHandler : IRequestHandler<ActualizarCursoCommand, CursoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActualizarCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CursoIdDto> Handle(ActualizarCursoCommand request, CancellationToken cancellationToken)
    {
        Curso nuevo = ValidacionCapacitacion.ValidarCurso(request.Solicitud);
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, request.Id);
        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<Curso>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != request.Id);
        if (existe)
        {
            throw ValidacionCapacitacion.CodigoCursoDuplicado(codigo);
        }

        curso.Codigo = nuevo.Codigo;
        curso.Nombre = nuevo.Nombre;
        curso.Horas = nuevo.Horas;
        curso.Tipo = nuevo.Tipo;
        curso.Proveedor = nuevo.Proveedor;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsCodigoCursoDuplicado(excepcion))
        {
            throw ValidacionCapacitacion.CodigoCursoDuplicado(codigo);
        }

        return new CursoIdDto { Id = curso.Id };
    }
}
