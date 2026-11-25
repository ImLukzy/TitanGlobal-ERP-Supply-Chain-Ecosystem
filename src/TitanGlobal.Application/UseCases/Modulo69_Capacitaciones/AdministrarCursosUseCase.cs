using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

public class AdministrarCursosUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarCursosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaCursosDto> ListarAsync(
        string? busqueda,
        string? tipo,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        string tipoNormalizado = tipo?.Trim() ?? string.Empty;
        ValidacionCapacitacion.ValidarPagina(pagina, tamanoPagina);
        ValidacionCapacitacion.ValidarTipoFiltro(tipoNormalizado);
        IQueryable<Curso> consulta = _unidad.Repository<Curso>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(curso => curso.Codigo.ToLower().Contains(termino)
                || curso.Nombre.ToLower().Contains(termino));
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(curso => curso.Tipo == tipoNormalizado);
        }

        if (activo is not null)
        {
            bool estaActivo = activo.Value;
            consulta = consulta.Where(curso => curso.Activo == estaActivo);
        }

        int total = await consulta.CountAsync();
        List<Curso> cursos = await consulta
            .OrderBy(curso => curso.Codigo)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();
        List<CursoListadoDto> items = new List<CursoListadoDto>();
        foreach (Curso curso in cursos)
        {
            items.Add(Convertir(curso));
        }

        return new PaginaCursosDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<CursoListadoDto> ObtenerAsync(int id)
    {
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, id);
        return Convertir(curso);
    }

    public async Task<CursoIdDto> CrearAsync(GuardarCursoRequestDto solicitud)
    {
        Curso curso = ValidacionCapacitacion.ValidarCurso(solicitud);
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

    public async Task<CursoIdDto> ActualizarAsync(int id, GuardarCursoRequestDto solicitud)
    {
        Curso nuevo = ValidacionCapacitacion.ValidarCurso(solicitud);
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, id);
        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<Curso>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != id);
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

    public async Task<CursoIdDto> DesactivarAsync(int id)
    {
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, id);
        if (!curso.Activo)
        {
            throw ValidacionCapacitacion.CursoYaEnEstado(curso.Codigo, false);
        }

        curso.Activo = false;
        await _unidad.CompleteAsync();
        return new CursoIdDto { Id = curso.Id };
    }

    public async Task<CursoIdDto> ActivarAsync(int id)
    {
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, id);
        if (curso.Activo)
        {
            throw ValidacionCapacitacion.CursoYaEnEstado(curso.Codigo, true);
        }

        curso.Activo = true;
        await _unidad.CompleteAsync();
        return new CursoIdDto { Id = curso.Id };
    }

    private static CursoListadoDto Convertir(Curso curso)
    {
        return new CursoListadoDto
        {
            Id = curso.Id,
            Codigo = curso.Codigo,
            Nombre = curso.Nombre,
            Horas = curso.Horas,
            Tipo = curso.Tipo,
            Proveedor = curso.Proveedor,
            Activo = curso.Activo
        };
    }
}
