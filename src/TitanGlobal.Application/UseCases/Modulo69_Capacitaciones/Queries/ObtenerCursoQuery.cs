using MediatR;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ObtenerCursoQuery : IRequest<CursoListadoDto>
{
    public int Id { get; set; }
}

public class ObtenerCursoQueryHandler : IRequestHandler<ObtenerCursoQuery, CursoListadoDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerCursoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<CursoListadoDto> Handle(ObtenerCursoQuery request, CancellationToken cancellationToken)
    {
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, request.Id);
        return OperacionesCursos.Convertir(curso);
    }
}
