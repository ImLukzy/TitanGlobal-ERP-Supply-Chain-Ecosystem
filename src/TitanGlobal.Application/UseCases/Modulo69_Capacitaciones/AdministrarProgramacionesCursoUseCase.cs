using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

public class AdministrarProgramacionesCursoUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarProgramacionesCursoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaProgramacionesDto> ListarAsync(
        int? anio,
        int? cursoId,
        string? estado,
        int pagina,
        int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionCapacitacion.ValidarPagina(pagina, tamanoPagina);
        ValidacionCapacitacion.ValidarEstadoProgramacionFiltro(estadoNormalizado);
        if (cursoId is not null)
        {
            await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, cursoId.Value);
        }

        IQueryable<ProgramacionCurso> consulta = _unidad.Repository<ProgramacionCurso>().AsQueryable();
        if (anio is not null)
        {
            int anioFiltro = anio.Value;
            consulta = consulta.Where(programacion => programacion.Anio == anioFiltro);
        }

        if (cursoId is not null)
        {
            int idCurso = cursoId.Value;
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
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<ProgramacionDetalleDto> ObtenerAsync(int id)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        List<InscripcionCurso> inscripciones = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.Empleado)
            .Include(inscripcion => inscripcion.CertificacionCurso)
            .AsNoTracking()
            .Where(inscripcion => inscripcion.ProgramacionCursoId == id)
            .OrderBy(inscripcion => inscripcion.Id)
            .ToListAsync();
        DateOnly hoy = ValidacionCapacitacion.Hoy();
        ProgramacionDetalleDto detalle = new ProgramacionDetalleDto
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
            Lugar = programacion.Lugar,
            Estado = programacion.Estado
        };
        foreach (InscripcionCurso inscripcion in inscripciones)
        {
            CertificacionDto? certificacion = null;
            if (inscripcion.CertificacionCurso is not null)
            {
                certificacion = new CertificacionDto
                {
                    Id = inscripcion.CertificacionCurso.Id,
                    Codigo = inscripcion.CertificacionCurso.Codigo,
                    FechaEmision = inscripcion.CertificacionCurso.FechaEmision,
                    FechaVencimiento = inscripcion.CertificacionCurso.FechaVencimiento,
                    Vencida = ConsultarCapacitacionesUseCase.EstaVencida(inscripcion.CertificacionCurso.FechaVencimiento, hoy)
                };
            }

            detalle.Inscripciones.Add(new InscripcionDto
            {
                Id = inscripcion.Id,
                EmpleadoId = inscripcion.EmpleadoId,
                EmpleadoLegajo = inscripcion.Empleado.Legajo,
                EmpleadoNombre = ValidacionCapacitacion.NombreEmpleado(inscripcion.Empleado),
                Estado = inscripcion.Estado,
                Nota = inscripcion.Nota,
                FechaInscripcion = inscripcion.FechaInscripcion,
                Certificacion = certificacion
            });
        }

        detalle.Inscritos = inscripciones.Count(inscripcion => inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado);
        detalle.CupoLibre = programacion.Cupo - detalle.Inscritos;
        return detalle;
    }

    public async Task<ProgramacionIdDto> CrearAsync(GuardarProgramacionRequestDto solicitud)
    {
        ProgramacionCurso programacion = ValidacionCapacitacion.ValidarProgramacion(solicitud);
        Curso curso = await ValidacionCapacitacion.ObtenerCursoAsync(_unidad, solicitud.CursoId ?? 0);
        if (!curso.Activo)
        {
            throw ValidacionCapacitacion.CursoInactivo(curso.Codigo);
        }

        programacion.CursoId = curso.Id;
        await _unidad.Repository<ProgramacionCurso>().AddAsync(programacion);
        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }

    public async Task<InscripcionIdDto> InscribirAsync(int id, InscribirEmpleadoRequestDto solicitud)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        int empleadoId = solicitud.EmpleadoId ?? 0;
        Empleado empleado = await ValidacionCapacitacion.ObtenerEmpleadoAsync(_unidad, empleadoId);
        if (empleado.Estado != ValidacionCapacitacion.EmpleadoActivo)
        {
            throw ValidacionCapacitacion.EmpleadoCesado(empleadoId);
        }

        InscripcionCurso? existente = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .SingleOrDefaultAsync(inscripcion => inscripcion.ProgramacionCursoId == id && inscripcion.EmpleadoId == empleadoId);
        if (existente is not null && existente.Estado != ValidacionCapacitacion.InscripcionRetirado)
        {
            throw ValidacionCapacitacion.InscripcionDuplicada(empleadoId);
        }

        int inscritos = await ValidacionCapacitacion.ContarInscritosAsync(_unidad, id);
        if (inscritos >= programacion.Cupo)
        {
            throw ValidacionCapacitacion.SinCupo(id);
        }

        InscripcionCurso inscripcionNueva = existente ?? new InscripcionCurso
        {
            ProgramacionCursoId = id,
            EmpleadoId = empleadoId
        };
        inscripcionNueva.Estado = ValidacionCapacitacion.InscripcionInscrito;
        inscripcionNueva.Nota = null;
        inscripcionNueva.FechaInscripcion = ValidacionCapacitacion.Hoy();
        try
        {
            if (existente is null)
            {
                await _unidad.Repository<InscripcionCurso>().AddAsync(inscripcionNueva);
            }

            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsInscripcionDuplicada(excepcion))
        {
            throw ValidacionCapacitacion.InscripcionDuplicada(empleadoId);
        }

        return new InscripcionIdDto { Id = inscripcionNueva.Id };
    }

    public async Task<InscripcionIdDto> RetirarAsync(int id, int inscripcionId)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        InscripcionCurso inscripcion = await ObtenerInscripcionAsync(id, inscripcionId);
        bool yaIniciada = ValidacionCapacitacion.Hoy() >= programacion.FechaInicio;
        if (inscripcion.Estado != ValidacionCapacitacion.InscripcionInscrito || yaIniciada)
        {
            throw ValidacionCapacitacion.Error409("Solo se puede retirar una inscripción antes del inicio del curso.");
        }

        inscripcion.Estado = ValidacionCapacitacion.InscripcionRetirado;
        await _unidad.CompleteAsync();
        return new InscripcionIdDto { Id = inscripcion.Id };
    }

    public async Task<ProgramacionIdDto> RealizarAsync(int id)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        if (programacion.FechaFin > ValidacionCapacitacion.Hoy())
        {
            string fechaFin = ValidacionCapacitacion.Fecha(programacion.FechaFin);
            throw ValidacionCapacitacion.Error409($"La programación {id} termina el {fechaFin} y aún no se puede marcar como realizada.");
        }

        programacion.Estado = ValidacionCapacitacion.ProgramacionRealizada;
        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }

    public async Task<ProgramacionIdDto> CancelarAsync(int id)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        ValidacionCapacitacion.ExigirProgramada(programacion);
        List<InscripcionCurso> inscripciones = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Where(inscripcion => inscripcion.ProgramacionCursoId == id
                && inscripcion.Estado != ValidacionCapacitacion.InscripcionRetirado)
            .ToListAsync();
        foreach (InscripcionCurso inscripcion in inscripciones)
        {
            inscripcion.Estado = ValidacionCapacitacion.InscripcionRetirado;
        }

        programacion.Estado = ValidacionCapacitacion.ProgramacionCancelada;
        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }

    // Todo se valida antes de modificar nada: un resultado inválido no deja cambios parciales.
    public async Task<ProgramacionIdDto> RegistrarResultadosAsync(int id, RegistrarResultadosRequestDto solicitud)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        if (programacion.Estado != ValidacionCapacitacion.ProgramacionRealizada)
        {
            throw ValidacionCapacitacion.Error409($"La programación {id} debe estar realizada para registrar resultados.");
        }

        List<InscripcionCurso> inscripciones = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .Include(inscripcion => inscripcion.CertificacionCurso)
            .Where(inscripcion => inscripcion.ProgramacionCursoId == id)
            .ToListAsync();
        List<(InscripcionCurso Inscripcion, ResultadoInscripcionRequestDto Resultado)> cambios =
            new List<(InscripcionCurso Inscripcion, ResultadoInscripcionRequestDto Resultado)>();
        foreach (ResultadoInscripcionRequestDto resultado in solicitud.Resultados ?? new List<ResultadoInscripcionRequestDto>())
        {
            int inscripcionId = resultado.InscripcionId ?? 0;
            InscripcionCurso? inscripcion = inscripciones.FirstOrDefault(entidad => entidad.Id == inscripcionId);
            if (inscripcion is null || inscripcion.Estado == ValidacionCapacitacion.InscripcionRetirado)
            {
                throw ValidacionCapacitacion.InscripcionInexistente(inscripcionId);
            }

            ValidacionCapacitacion.ValidarNota(resultado.Nota);
            if (inscripcion.CertificacionCurso is not null)
            {
                throw ValidacionCapacitacion.CertificacionDuplicada(inscripcionId);
            }

            cambios.Add((inscripcion, resultado));
        }

        foreach ((InscripcionCurso inscripcion, ResultadoInscripcionRequestDto resultado) in cambios)
        {
            inscripcion.Estado = resultado.Aprobado
                ? ValidacionCapacitacion.InscripcionAprobado
                : ValidacionCapacitacion.InscripcionDesaprobado;
            inscripcion.Nota = resultado.Nota;
        }

        await _unidad.CompleteAsync();
        return new ProgramacionIdDto { Id = programacion.Id };
    }

    public async Task<CertificacionIdDto> EmitirCertificacionAsync(int id, int inscripcionId, EmitirCertificacionRequestDto solicitud)
    {
        ProgramacionCurso programacion = await ValidacionCapacitacion.ObtenerProgramacionAsync(_unidad, id);
        InscripcionCurso inscripcion = await ObtenerInscripcionAsync(id, inscripcionId);
        if (inscripcion.Estado != ValidacionCapacitacion.InscripcionAprobado)
        {
            throw ValidacionCapacitacion.Error409($"La inscripción {inscripcionId} no está aprobada y no admite certificación.");
        }

        bool tieneCertificacion = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .AnyAsync(certificacion => certificacion.InscripcionCursoId == inscripcionId);
        if (tieneCertificacion)
        {
            throw ValidacionCapacitacion.CertificacionDuplicada(inscripcionId);
        }

        string codigo = solicitud.Codigo?.Trim() ?? string.Empty;
        if (codigo.Length == 0 || codigo.Length > ValidacionCapacitacion.MaximoCodigoCertificacion)
        {
            throw ValidacionCapacitacion.Error400("El código de certificación es obligatorio y no puede superar 40 caracteres.");
        }

        if (solicitud.FechaEmision is null || solicitud.FechaEmision.Value < programacion.FechaFin)
        {
            throw ValidacionCapacitacion.Error400("La fecha de emisión es obligatoria y no puede ser anterior al fin del curso.");
        }

        DateOnly emision = solicitud.FechaEmision.Value;
        if (solicitud.FechaVencimiento is not null && solicitud.FechaVencimiento.Value < emision)
        {
            throw ValidacionCapacitacion.Error400("La fecha de vencimiento no puede ser anterior a la de emisión.");
        }

        codigo = codigo.ToUpperInvariant();
        bool codigoExiste = await _unidad.Repository<CertificacionCurso>().AsQueryable()
            .AnyAsync(certificacion => certificacion.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionCapacitacion.CodigoCertificacionDuplicado(codigo);
        }

        CertificacionCurso nueva = new CertificacionCurso
        {
            InscripcionCursoId = inscripcionId,
            Codigo = codigo,
            FechaEmision = emision,
            FechaVencimiento = solicitud.FechaVencimiento
        };
        try
        {
            await _unidad.Repository<CertificacionCurso>().AddAsync(nueva);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsCertificacionDuplicada(excepcion))
        {
            throw ValidacionCapacitacion.CertificacionDuplicada(inscripcionId);
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionCapacitacion.EsCodigoCertificacionDuplicado(excepcion))
        {
            throw ValidacionCapacitacion.CodigoCertificacionDuplicado(codigo);
        }

        return new CertificacionIdDto { Id = nueva.Id };
    }

    private async Task<InscripcionCurso> ObtenerInscripcionAsync(int programacionId, int inscripcionId)
    {
        InscripcionCurso? inscripcion = await _unidad.Repository<InscripcionCurso>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == inscripcionId && entidad.ProgramacionCursoId == programacionId);
        if (inscripcion is null)
        {
            throw ValidacionCapacitacion.InscripcionInexistente(inscripcionId);
        }

        return inscripcion;
    }
}
