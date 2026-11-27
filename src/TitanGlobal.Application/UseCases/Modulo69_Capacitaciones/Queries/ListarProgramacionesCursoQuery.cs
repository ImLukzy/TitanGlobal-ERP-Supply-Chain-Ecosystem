using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ListarProgramacionesCursoQuery : IRequest<PaginaProgramacionesDto>
{
    public int? Anio { get; set; }

    public int? CursoId { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarProgramacionesCursoQueryHandler : IRequestHandler<ListarProgramacionesCursoQuery, PaginaProgramacionesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarProgramacionesCursoQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaProgramacionesDto> Handle(ListarProgramacionesCursoQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionCapacitacion.ValidarPagina(request.Pagina, request.TamanoPagina);
        ValidacionCapacitacion.ValidarEstadoProgramacionFiltro(estadoNormalizado);
        if (request.CursoId is not null)
        {
            await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, request.CursoId.Value);
        }

        IQueryable<ProgramacionCurso> consulta = _unidad.Repository<ProgramacionCurso>().AsQueryable();
        if (request.Anio is not null)
        {
            int anioFiltro = request.Anio.Value;
            consulta = consulta.Where(programacion => programacion.Anio == anioFiltro);
        }

        if (request.CursoId is not null)
        {
            int idCurso = request.CursoId.Value;
            consulta = consulta.Where(programacion => programacion.CursoId == idCurso);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(programacion => programacion.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<ProgramacionCurso> programaciones = await consulta
            .Include(programacion => programacion.Curso)
            .Include(programacion => programacion.InscripcionesCurso)
            .AsNoTracking()
            .OrderByDescending(programacion => programacion.FechaInicio)
            .ThenBy(programacion => programacion.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<ProgramacionListadoDto> items = new List<ProgramacionListadoDto>();
        foreach (ProgramacionCurso programacion in programaciones)
        {
            int inscritos = programacion.InscripcionesCurso
                .Count(inscripcion => inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado);
            items.Add(new ProgramacionListadoDto
            {
                Id = programacion.Id,
                CursoId = programacion.CursoId,
                CursoCodigo = programacion.Curso.Codigo,
                CursoNombre = programacion.Curso.Nombre,
                Horas = programacion.Curso.Horas,
                Anio = programacion.Anio,
                FechaInicio = programacion.FechaInicio,
                FechaFin = programacion.FechaFin,
                Cupo = programacion.Cupo,
                Inscritos = inscritos,
                CupoLibre = programacion.Cupo - inscritos,
                Lugar = programacion.Lugar,
                Estado = programacion.Estado
            });
        }

        return new PaginaProgramacionesDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
