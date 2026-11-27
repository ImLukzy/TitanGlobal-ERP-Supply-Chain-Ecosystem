using MediatR;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class ActivarCursoCommand : IRequest<CursoIdDto>
{
    public int Id { get; set; }
}

public class ActivarCursoCommandHandler : IRequestHandler<ActivarCursoCommand, CursoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public ActivarCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CursoIdDto> Handle(ActivarCursoCommand request, CancellationToken cancellationToken)
    {
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, request.Id);
        if (curso.Activo)
        {
            throw ValidacionCapacitacion.CursoYaEnEstado(curso.Codigo, true);
        }

        curso.Activo = true;
        await _unidad.CompleteAsync();
        return new CursoIdDto { Id = curso.Id };
    }
}
