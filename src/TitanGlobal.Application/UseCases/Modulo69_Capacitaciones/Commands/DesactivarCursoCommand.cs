using MediatR;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Commands;

public class DesactivarCursoCommand : IRequest<CursoIdDto>
{
    public int Id { get; set; }
}

public class DesactivarCursoCommandHandler : IRequestHandler<DesactivarCursoCommand, CursoIdDto>
{
    private readonly IUnitOfWork _unidad;

    public DesactivarCursoCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CursoIdDto> Handle(DesactivarCursoCommand request, CancellationToken cancellationToken)
    {
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, request.Id);
        if (!curso.Activo)
        {
            throw ValidacionCapacitacion.CursoYaEnEstado(curso.Codigo, false);
        }

        curso.Activo = false;
        await _unidad.CompleteAsync();
        return new CursoIdDto { Id = curso.Id };
    }
}
