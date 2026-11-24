using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno;

public class AdministrarEvaluacionesUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarEvaluacionesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEvaluacionesDto> ListarAsync(
        int? cicloId,
        int? empleadoId,
        string? estado,
        string? busqueda,
        int pagina,
        int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionEvaluacion.ValidarPagina(pagina, tamanoPagina);
        ValidacionEvaluacion.ValidarEstadoEvaluacion(estadoNormalizado);
        if (cicloId is not null)
        {
            bool existeCiclo = await _unidad.Repository<CicloEvaluacion>().AsQueryable()
                .AnyAsync(ciclo => ciclo.Id == cicloId.Value);
            if (!existeCiclo)
            {
                throw ValidacionEvaluacion.CicloInexistente(cicloId.Value);
            }
        }

        if (empleadoId is not null)
        {
            await ValidacionEvaluacion.ObtenerEmpleadoAsync(_unidad, empleadoId.Value);
        }

        IQueryable<Evaluacion> consulta = _unidad.Repository<Evaluacion>().AsQueryable();
        if (cicloId is not null)
        {
            int idCiclo = cicloId.Value;
            consulta = consulta.Where(evaluacion => evaluacion.CicloEvaluacionId == idCiclo);
        }

        if (empleadoId is not null)
        {
            int idEmpleado = empleadoId.Value;
            consulta = consulta.Where(evaluacion => evaluacion.EmpleadoId == idEmpleado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(evaluacion => evaluacion.Estado == estadoNormalizado);
        }

        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(evaluacion => evaluacion.CicloEvaluacion.Codigo.ToLower().Contains(termino)
                || evaluacion.Empleado.Nombres.ToLower().Contains(termino)
                || evaluacion.Empleado.Apellidos.ToLower().Contains(termino)
                || evaluacion.Empleado.Legajo.ToLower().Contains(termino));
        }

        int total = await consulta.CountAsync();
        List<EvaluacionListadoDto> items = await consulta
            .OrderByDescending(evaluacion => evaluacion.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(evaluacion => new EvaluacionListadoDto
            {
                Id = evaluacion.Id,
                CicloId = evaluacion.CicloEvaluacionId,
                CicloCodigo = evaluacion.CicloEvaluacion.Codigo,
                EmpleadoId = evaluacion.EmpleadoId,
                EmpleadoLegajo = evaluacion.Empleado.Legajo,
                EmpleadoNombre = evaluacion.Empleado.Apellidos + ", " + evaluacion.Empleado.Nombres,
                EvaluadorId = evaluacion.EvaluadorId,
                EvaluadorNombre = evaluacion.Evaluador.Apellidos + ", " + evaluacion.Evaluador.Nombres,
                Estado = evaluacion.Estado,
                PuntajeFinal = evaluacion.PuntajeFinal
            })
            .ToListAsync();
        foreach (EvaluacionListadoDto item in items)
        {
            item.Etiqueta = item.PuntajeFinal is null ? null : ValidacionEvaluacion.Etiqueta(item.PuntajeFinal.Value);
        }

        return new PaginaEvaluacionesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<EvaluacionDetalleDto> ObtenerAsync(int id)
    {
        EvaluacionDetalleDto? detalle = await _unidad.Repository<Evaluacion>().AsQueryable()
            .Where(evaluacion => evaluacion.Id == id)
            .Select(evaluacion => new EvaluacionDetalleDto
            {
                Id = evaluacion.Id,
                CicloId = evaluacion.CicloEvaluacionId,
                CicloCodigo = evaluacion.CicloEvaluacion.Codigo,
                EmpleadoId = evaluacion.EmpleadoId,
                EmpleadoLegajo = evaluacion.Empleado.Legajo,
                EmpleadoNombre = evaluacion.Empleado.Apellidos + ", " + evaluacion.Empleado.Nombres,
                EvaluadorId = evaluacion.EvaluadorId,
                EvaluadorNombre = evaluacion.Evaluador.Apellidos + ", " + evaluacion.Evaluador.Nombres,
                Estado = evaluacion.Estado,
                Comentario = evaluacion.Comentario,
                PuntajeFinal = evaluacion.PuntajeFinal,
                FechaEnvio = evaluacion.FechaEnvio,
                FechaCierre = evaluacion.FechaCierre
            })
            .SingleOrDefaultAsync();
        if (detalle is null)
        {
            throw ValidacionEvaluacion.EvaluacionInexistente(id);
        }

        detalle.Etiqueta = detalle.PuntajeFinal is null ? null : ValidacionEvaluacion.Etiqueta(detalle.PuntajeFinal.Value);
        List<CompetenciaEvaluacion> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(competencia => competencia.CicloEvaluacionId == detalle.CicloId)
            .OrderBy(competencia => competencia.Id)
            .ToListAsync();
        List<CalificacionEvaluacion> calificaciones = await _unidad.Repository<CalificacionEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(calificacion => calificacion.EvaluacionId == id)
            .ToListAsync();
        detalle.Calificaciones = competencias
            .Select(competencia => new CalificacionEvaluacionDto
            {
                CompetenciaId = competencia.Id,
                Competencia = competencia.Nombre,
                Peso = competencia.Peso,
                Calificacion = calificaciones
                    .Where(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id)
                    .Select(calificacion => (int?)calificacion.Calificacion)
                    .FirstOrDefault()
            })
            .ToList();
        return detalle;
    }

    public async Task<EvaluacionIdDto> CrearAsync(CrearEvaluacionRequestDto solicitud)
    {
        if (solicitud.CicloId is null || solicitud.EmpleadoId is null || solicitud.EvaluadorId is null)
        {
            throw ValidacionEvaluacion.Error400("El ciclo, el empleado y el evaluador son obligatorios.");
        }

        int cicloId = solicitud.CicloId.Value;
        int empleadoId = solicitud.EmpleadoId.Value;
        int evaluadorId = solicitud.EvaluadorId.Value;
        if (empleadoId == evaluadorId)
        {
            throw ValidacionEvaluacion.Error400("El evaluador no puede ser el propio evaluado.");
        }

        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, cicloId);
        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        ValidacionEvaluacion.ExigirVentana(ciclo);
        await ValidacionEvaluacion.ObtenerEmpleadoAsync(_unidad, empleadoId);
        bool conContrato = await ValidacionEvaluacion.TuvoContratoEnPeriodoAsync(
            _unidad,
            empleadoId,
            ciclo.FechaInicioPeriodo,
            ciclo.FechaFinPeriodo);
        if (!conContrato)
        {
            throw ValidacionEvaluacion.Error409($"El empleado {empleadoId} no tuvo contrato vigente durante el periodo del ciclo.");
        }

        Empleado evaluador = await ValidacionEvaluacion.ObtenerEmpleadoAsync(_unidad, evaluadorId);
        if (evaluador.Estado != ValidacionEvaluacion.EmpleadoActivo)
        {
            throw ValidacionEvaluacion.Error409($"El evaluador {evaluadorId} no está activo.");
        }

        bool existe = await _unidad.Repository<Evaluacion>().AsQueryable()
            .AnyAsync(otra => otra.CicloEvaluacionId == cicloId && otra.EmpleadoId == empleadoId);
        if (existe)
        {
            throw ValidacionEvaluacion.EvaluacionDuplicada(empleadoId, ciclo.Codigo);
        }

        Evaluacion evaluacion = new Evaluacion
        {
            CicloEvaluacionId = cicloId,
            EmpleadoId = empleadoId,
            EvaluadorId = evaluadorId,
            Estado = ValidacionEvaluacion.EvaluacionBorrador
        };
        try
        {
            await _unidad.Repository<Evaluacion>().AddAsync(evaluacion);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsEvaluacionDuplicada(excepcion))
        {
            throw ValidacionEvaluacion.EvaluacionDuplicada(empleadoId, ciclo.Codigo);
        }

        return new EvaluacionIdDto { Id = evaluacion.Id };
    }

    // Reemplaza las calificaciones existentes de una evaluación en borrador.
    public async Task<EvaluacionIdDto> GuardarCalificacionesAsync(int id, GuardarCalificacionesRequestDto solicitud)
    {
        Evaluacion evaluacion = await ObtenerEvaluacionAsync(id);
        ExigirBorrador(evaluacion);
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, evaluacion.CicloEvaluacionId);
        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        string comentario = solicitud.Comentario?.Trim() ?? string.Empty;
        if (comentario.Length > ValidacionEvaluacion.MaximoComentario)
        {
            throw ValidacionEvaluacion.Error400("El comentario no puede superar 500 caracteres.");
        }

        List<int> idsCompetencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .Where(competencia => competencia.CicloEvaluacionId == ciclo.Id)
            .Select(competencia => competencia.Id)
            .ToListAsync();
        Dictionary<int, int> nuevas = new Dictionary<int, int>();
        foreach (CalificacionRequestDto entrada in solicitud.Calificaciones ?? new List<CalificacionRequestDto>())
        {
            decimal? nota = entrada.Calificacion;
            if (nota is null || nota.Value < 1m || nota.Value > 5m || nota.Value != decimal.Truncate(nota.Value))
            {
                throw ValidacionEvaluacion.Error400("La calificación debe ser un número entero de 1 a 5.");
            }

            int competenciaId = entrada.CompetenciaId ?? 0;
            if (!idsCompetencias.Contains(competenciaId))
            {
                throw ValidacionEvaluacion.Error400($"La competencia {competenciaId} no pertenece al ciclo de la evaluación.");
            }

            nuevas[competenciaId] = (int)nota.Value;
        }

        List<CalificacionEvaluacion> anteriores = await _unidad.Repository<CalificacionEvaluacion>().AsQueryable()
            .Where(calificacion => calificacion.EvaluacionId == id)
            .ToListAsync();
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        foreach (CalificacionEvaluacion anterior in anteriores)
        {
            _unidad.Repository<CalificacionEvaluacion>().Remove(anterior);
        }

        await _unidad.CompleteAsync();
        foreach (KeyValuePair<int, int> nueva in nuevas)
        {
            await _unidad.Repository<CalificacionEvaluacion>().AddAsync(new CalificacionEvaluacion
            {
                EvaluacionId = id,
                CompetenciaEvaluacionId = nueva.Key,
                Calificacion = nueva.Value
            });
        }

        evaluacion.Comentario = comentario.Length == 0 ? null : comentario;
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new EvaluacionIdDto { Id = evaluacion.Id };
    }

    public async Task<EvaluacionIdDto> EnviarAsync(int id)
    {
        Evaluacion evaluacion = await ObtenerEvaluacionAsync(id);
        ExigirBorrador(evaluacion);
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, evaluacion.CicloEvaluacionId);
        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        ValidacionEvaluacion.ExigirVentana(ciclo);
        List<CompetenciaEvaluacion> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(competencia => competencia.CicloEvaluacionId == ciclo.Id)
            .ToListAsync();
        List<CalificacionEvaluacion> calificaciones = await _unidad.Repository<CalificacionEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(calificacion => calificacion.EvaluacionId == id)
            .ToListAsync();
        int faltan = competencias.Count(competencia => !calificaciones.Any(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id));
        if (faltan > 0)
        {
            throw ValidacionEvaluacion.Error409($"Faltan calificar {faltan} competencias para enviar la evaluación.");
        }

        List<(decimal Peso, int Calificacion)> notas = competencias
            .Select(competencia => (
                competencia.Peso,
                calificaciones.First(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id).Calificacion))
            .ToList();
        evaluacion.PuntajeFinal = ValidacionEvaluacion.CalcularPuntaje(notas);
        evaluacion.Estado = ValidacionEvaluacion.EvaluacionEnviada;
        evaluacion.FechaEnvio = DateTime.UtcNow;
        await _unidad.CompleteAsync();
        return new EvaluacionIdDto { Id = evaluacion.Id };
    }

    public async Task<EvaluacionIdDto> CerrarAsync(int id)
    {
        Evaluacion evaluacion = await ObtenerEvaluacionAsync(id);
        if (evaluacion.Estado != ValidacionEvaluacion.EvaluacionEnviada)
        {
            throw ValidacionEvaluacion.Error409($"La evaluación {id} debe estar enviada para cerrarse.");
        }

        evaluacion.Estado = ValidacionEvaluacion.EvaluacionCerrada;
        evaluacion.FechaCierre = DateTime.UtcNow;
        await _unidad.CompleteAsync();
        return new EvaluacionIdDto { Id = evaluacion.Id };
    }

    private async Task<Evaluacion> ObtenerEvaluacionAsync(int id)
    {
        Evaluacion? evaluacion = await _unidad.Repository<Evaluacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (evaluacion is null)
        {
            throw ValidacionEvaluacion.EvaluacionInexistente(id);
        }

        return evaluacion;
    }

    private static void ExigirBorrador(Evaluacion evaluacion)
    {
        if (evaluacion.Estado != ValidacionEvaluacion.EvaluacionBorrador)
        {
            throw ValidacionEvaluacion.Error409($"La evaluación {evaluacion.Id} no está en borrador.");
        }
    }
}
