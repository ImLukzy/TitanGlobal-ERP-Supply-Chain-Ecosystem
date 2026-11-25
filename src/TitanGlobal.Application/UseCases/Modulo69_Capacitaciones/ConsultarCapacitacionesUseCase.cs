using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

public class ConsultarCapacitacionesUseCase
{
    private readonly IUnitOfWork _unidad;

    public ConsultarCapacitacionesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // K4: una certificación está vencida cuando su vencimiento es anterior a hoy; sin vencimiento no vence.
    public static bool EstaVencida(DateOnly? fechaVencimiento, DateOnly hoy)
    {
        return fechaVencimiento is not null && fechaVencimiento.Value < hoy;
    }

    // K5: horas de los cursos aprobados cuya programación terminó en el año, por empleado activo.
    public async Task<PaginaHorasDto> ListarHorasAsync(int? anio, string? busqueda, int pagina, int tamanoPagina)
    {
        ValidacionCapacitacion.ValidarPagina(pagina, tamanoPagina);
        int anioConsulta = ValidacionCapacitacion.ResolverAnio(anio);
        IQueryable<Empleado> consulta = _unidad.Repository<Empleado>().AsQueryable()
            .Where(empleado => empleado.Estado == ValidacionCapacitacion.EmpleadoActivo);
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
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
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<int> ids = empleados.Select(empleado => empleado.Id).ToList();
        List<InscripcionCurso> aprobadas = await ConsultarAprobadasAsync(anioConsulta, ids);
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<CapacitacionEmpleadoDto> ObtenerEmpleadoAsync(int empleadoId, int? anio)
    {
        Empleado empleado = await ValidacionCapacitacion.ObtenerEmpleadoAsync(_unidad, empleadoId);
        int anioConsulta = ValidacionCapacitacion.ResolverAnio(anio);
        DateOnly inicioAnio = new DateOnly(anioConsulta, 1, 1);
        DateOnly finAnio = new DateOnly(anioConsulta, 12, 31);
        List<InscripcionCurso> delAnio = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.ProgramacionCurso)
            .ThenInclude(programacion => programacion.Curso)
            .AsNoTracking()
            .Where(inscripcion => inscripcion.EmpleadoId == empleadoId
                && inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado
                && inscripcion.ProgramacionCurso.FechaFin >= inicioAnio
                && inscripcion.ProgramacionCurso.FechaFin <= finAnio)
            .OrderBy(inscripcion => inscripcion.ProgramacionCurso.FechaInicio)
            .ThenBy(inscripcion => inscripcion.Id)
            .ToListAsync();
        List<CertificacionCurso> certificaciones = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .Include(certificacion => certificacion.InscripcionCurso)
            .ThenInclude(inscripcion => inscripcion.ProgramacionCurso)
            .ThenInclude(programacion => programacion.Curso)
            .AsNoTracking()
            .Where(certificacion => certificacion.InscripcionCurso.EmpleadoId == empleadoId)
            .OrderBy(certificacion => certificacion.FechaEmision)
            .ThenBy(certificacion => certificacion.Id)
            .ToListAsync();
        DateOnly hoy = ValidacionCapacitacion.Hoy();
        CapacitacionEmpleadoDto resultado = new CapacitacionEmpleadoDto
        {
            EmpleadoId = empleado.Id,
            EmpleadoLegajo = empleado.Legajo,
            EmpleadoNombre = ValidacionCapacitacion.NombreEmpleado(empleado),
            Anio = anioConsulta,
            Horas = delAnio
                .Where(inscripcion => inscripcion.Estado == ValidacionCapacitacion.InscripcionAprobado)
                .Sum(inscripcion => inscripcion.ProgramacionCurso.Curso.Horas)
        };
        foreach (InscripcionCurso inscripcion in delAnio)
        {
            resultado.Cursos.Add(new CursoEmpleadoDto
            {
                InscripcionId = inscripcion.Id,
                ProgramacionId = inscripcion.ProgramacionCursoId,
                CursoCodigo = inscripcion.ProgramacionCurso.Curso.Codigo,
                CursoNombre = inscripcion.ProgramacionCurso.Curso.Nombre,
                Horas = inscripcion.ProgramacionCurso.Curso.Horas,
                FechaInicio = inscripcion.ProgramacionCurso.FechaInicio,
                FechaFin = inscripcion.ProgramacionCurso.FechaFin,
                Estado = inscripcion.Estado,
                Nota = inscripcion.Nota
            });
        }

        foreach (CertificacionCurso certificacion in certificaciones)
        {
            resultado.Certificaciones.Add(new CertificacionEmpleadoDto
            {
                Id = certificacion.Id,
                InscripcionId = certificacion.InscripcionCursoId,
                CursoCodigo = certificacion.InscripcionCurso.ProgramacionCurso.Curso.Codigo,
                CursoNombre = certificacion.InscripcionCurso.ProgramacionCurso.Curso.Nombre,
                Codigo = certificacion.Codigo,
                FechaEmision = certificacion.FechaEmision,
                FechaVencimiento = certificacion.FechaVencimiento,
                Vencida = EstaVencida(certificacion.FechaVencimiento, hoy)
            });
        }

        return resultado;
    }

    public async Task<ResumenCapacitacionesDto> ResumenAsync(int? anio)
    {
        int anioConsulta = ValidacionCapacitacion.ResolverAnio(anio);
        DateOnly hoy = ValidacionCapacitacion.Hoy();
        int cursosActivos = await _unidad.Repository<Curso>().AsQueryable().CountAsync(curso => curso.Activo);
        IQueryable<ProgramacionCurso> programaciones = _unidad.Repository<ProgramacionCurso>().AsQueryable()
            .Where(programacion => programacion.Anio == anioConsulta);
        int programadas = await programaciones
            .CountAsync(programacion => programacion.Estado == ValidacionCapacitacion.ProgramacionProgramada);
        int realizadas = await programaciones
            .CountAsync(programacion => programacion.Estado == ValidacionCapacitacion.ProgramacionRealizada);
        int inscritos = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .CountAsync(inscripcion => inscripcion.ProgramacionCurso.Anio == anioConsulta
                && inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado);
        List<InscripcionCurso> aprobadas = await ConsultarAprobadasAsync(anioConsulta, null);
        int vencidas = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .CountAsync(certificacion => certificacion.FechaVencimiento != null && certificacion.FechaVencimiento < hoy);
        return new ResumenCapacitacionesDto
        {
            Anio = anioConsulta,
            CursosActivos = cursosActivos,
            Programadas = programadas,
            Realizadas = realizadas,
            Inscritos = inscritos,
            HorasTotales = aprobadas.Sum(inscripcion => inscripcion.ProgramacionCurso.Curso.Horas),
            CertificacionesVencidas = vencidas
        };
    }

    private async Task<List<InscripcionCurso>> ConsultarAprobadasAsync(int anio, List<int>? empleadoIds)
    {
        DateOnly inicioAnio = new DateOnly(anio, 1, 1);
        DateOnly finAnio = new DateOnly(anio, 12, 31);
        IQueryable<InscripcionCurso> consulta = _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.ProgramacionCurso)
            .ThenInclude(programacion => programacion.Curso)
            .AsNoTracking()
            .Where(inscripcion => inscripcion.Estado == ValidacionCapacitacion.InscripcionAprobado
                && inscripcion.ProgramacionCurso.FechaFin >= inicioAnio
                && inscripcion.ProgramacionCurso.FechaFin <= finAnio);
        if (empleadoIds is not null)
        {
            consulta = consulta.Where(inscripcion => empleadoIds.Contains(inscripcion.EmpleadoId));
        }

        return await consulta.ToListAsync();
    }
}
