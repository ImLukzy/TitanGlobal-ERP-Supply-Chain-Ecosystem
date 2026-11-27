using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios.Queries;

public class ListarTurnosQuery : IRequest<PaginaTurnosDto>
{
    public string? Busqueda { get; set; }

    public string? Tipo { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarTurnosQueryHandler : IRequestHandler<ListarTurnosQuery, PaginaTurnosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarTurnosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaTurnosDto> Handle(ListarTurnosQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        ValidacionTurno.ValidarListadoTurnos(request.Pagina, request.TamanoPagina, tipoNormalizado);
        IQueryable<Turno> consulta = _unidad.Repository<Turno>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(turno => turno.Codigo.ToLower().Contains(termino)
                || turno.Nombre.ToLower().Contains(termino));
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(turno => turno.Tipo == tipoNormalizado);
        }

        if (request.Activo is not null)
        {
            bool estaActivo = request.Activo.Value;
            consulta = consulta.Where(turno => turno.Activo == estaActivo);
        }

        int total = await consulta.CountAsync();
        List<Turno> turnos = await consulta
            .OrderBy(turno => turno.Codigo)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<TurnoListadoDto> items = new List<TurnoListadoDto>();
        foreach (Turno turno in turnos)
        {
            TurnoListadoDto dto = new TurnoListadoDto();
            ValidacionTurno.CopiarEn(turno, dto);
            items.Add(dto);
        }

        return new PaginaTurnosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
