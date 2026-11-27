using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones.Queries;

public class ListarHorasQuery : IRequest<PaginaHorasDto>
{
    public int? Anio { get; set; }

    public string? Busqueda { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarHorasQueryHandler : IRequestHandler<ListarHorasQuery, PaginaHorasDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarHorasQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // K5: horas de los cursos aprobados cuya programación terminó en el año, por empleado activo.
    public async Task<PaginaHorasDto> Handle(ListarHorasQuery request, CancellationToken cancellationToken)
    {
        ValidacionCapacitacion.ValidarPagina(request.Pagina, request.TamanoPagina);
        int anioConsulta = ValidacionCapacitacion.ResolverAnio(request.Anio);
        IQueryable<Empleado> consulta = _unidad.Repository<Empleado>().AsQueryable()
            .Where(empleado => empleado.Estado == ValidacionCapacitacion.EmpleadoActivo);
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(empleado => empleado.Nombres.ToLower().Contains(termino)
                || empleado.Apellidos.ToLower().Contains(termino)
                || empleado.Legajo.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        List<Empleado> empleados = await consulta
            .AsNoTracking()
            .OrderBy(empleado => empleado.Apellidos)
            .ThenBy(empleado => empleado.Nombres)
            .ThenBy(empleado => empleado.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .ToListAsync();
        List<int> ids = empleados.Select(empleado => empleado.Id).ToList();
        List<InscripcionCurso> aprobadas = await OperacionesCapacitaciones.ConsultarAprobadasAsync(_unidad, anioConsulta, ids);
        List<HorasEmpleadoDto> items = new List<HorasEmpleadoDto>();
        foreach (Empleado empleado in empleados)
        {
            List<InscripcionCurso> propias = aprobadas.Where(inscripcion => inscripcion.EmpleadoId == empleado.Id).ToList();
            items.Add(new HorasEmpleadoDto
            {
                EmpleadoId = empleado.Id,
                EmpleadoLegajo = empleado.Legajo,
                EmpleadoNombre = ValidacionCapacitacion.NombreEmpleado(empleado),
                Cursos = propias.Count,
                Horas = propias.Sum(inscripcion => inscripcion.ProgramacionCurso.Curso.Horas)
            });
        }

        return new PaginaHorasDto
        {
            Anio = anioConsulta,
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
