using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno;

public class AdministrarCiclosEvaluacionUseCase
{
    private readonly IUnitOfWork _unidad;

    public AdministrarCiclosEvaluacionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaCiclosEvaluacionDto> ListarAsync(string? busqueda, string? estado, int pagina, int tamanoPagina)
    {
        string estadoNormalizado = estado?.Trim() ?? string.Empty;
        ValidacionEvaluacion.ValidarPagina(pagina, tamanoPagina);
        ValidacionEvaluacion.ValidarEstadoCiclo(estadoNormalizado);
        IQueryable<CicloEvaluacion> consulta = _unidad.Repository<CicloEvaluacion>().AsQueryable();
        string termino = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(ciclo => ciclo.Codigo.ToLower().Contains(termino)
                || ciclo.Nombre.ToLower().Contains(termino));
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(ciclo => ciclo.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<CicloEvaluacionListadoDto> items = await consulta
            .OrderByDescending(ciclo => ciclo.FechaInicioPeriodo)
            .ThenByDescending(ciclo => ciclo.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(ciclo => new CicloEvaluacionListadoDto
            {
                Id = ciclo.Id,
                Codigo = ciclo.Codigo,
                Nombre = ciclo.Nombre,
                Periodo = ciclo.Periodo,
                FechaInicioPeriodo = ciclo.FechaInicioPeriodo,
                FechaFinPeriodo = ciclo.FechaFinPeriodo,
                FechaApertura = ciclo.FechaApertura,
                FechaCierre = ciclo.FechaCierre,
                Estado = ciclo.Estado
            })
            .ToListAsync();
        return new PaginaCiclosEvaluacionDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }

    public async Task<CicloEvaluacionDetalleDto> ObtenerAsync(int id)
    {
        CicloEvaluacion ciclo = await ObtenerSinSeguimientoAsync(id);
        List<CompetenciaEvaluacionDto> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .Where(competencia => competencia.CicloEvaluacionId == id)
            .OrderBy(competencia => competencia.Id)
            .Select(competencia => new CompetenciaEvaluacionDto
            {
                Id = competencia.Id,
                Nombre = competencia.Nombre,
                Descripcion = competencia.Descripcion,
                Peso = competencia.Peso
            })
            .ToListAsync();
        List<string> estados = await _unidad.Repository<Evaluacion>().AsQueryable()
            .Where(evaluacion => evaluacion.CicloEvaluacionId == id)
            .Select(evaluacion => evaluacion.Estado)
            .ToListAsync();
        return new CicloEvaluacionDetalleDto
        {
            Id = ciclo.Id,
            Codigo = ciclo.Codigo,
            Nombre = ciclo.Nombre,
            Periodo = ciclo.Periodo,
            FechaInicioPeriodo = ciclo.FechaInicioPeriodo,
            FechaFinPeriodo = ciclo.FechaFinPeriodo,
            FechaApertura = ciclo.FechaApertura,
            FechaCierre = ciclo.FechaCierre,
            Estado = ciclo.Estado,
            SumaPesos = competencias.Sum(competencia => competencia.Peso),
            Competencias = competencias,
            Evaluaciones = new EvaluacionesPorEstadoDto
            {
                Borrador = estados.Count(estado => estado == ValidacionEvaluacion.EvaluacionBorrador),
                Enviada = estados.Count(estado => estado == ValidacionEvaluacion.EvaluacionEnviada),
                Cerrada = estados.Count(estado => estado == ValidacionEvaluacion.EvaluacionCerrada)
            }
        };
    }

    public async Task<ResultadosCicloEvaluacionDto> ObtenerResultadosAsync(int id)
    {
        await ObtenerSinSeguimientoAsync(id);
        List<CompetenciaEvaluacion> competencias = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AsNoTracking()
            .Where(competencia => competencia.CicloEvaluacionId == id)
            .OrderBy(competencia => competencia.Id)
            .ToListAsync();
        List<Evaluacion> evaluaciones = await _unidad.Repository<Evaluacion>().AsQueryable()
            .AsNoTracking()
            .Include(evaluacion => evaluacion.CalificacionesEvaluacion)
            .Where(evaluacion => evaluacion.CicloEvaluacionId == id
                && (evaluacion.Estado == ValidacionEvaluacion.EvaluacionEnviada
                    || evaluacion.Estado == ValidacionEvaluacion.EvaluacionCerrada))
            .ToListAsync();
        List<decimal> puntajes = evaluaciones.Select(evaluacion => evaluacion.PuntajeFinal ?? 0m).ToList();
        List<DistribucionPuntajeDto> distribucion = ValidacionEvaluacion.Etiquetas
            .Select(etiqueta => new DistribucionPuntajeDto
            {
                Etiqueta = etiqueta,
                Cantidad = puntajes.Count(puntaje => ValidacionEvaluacion.Etiqueta(puntaje) == etiqueta)
            })
            .ToList();
        List<PromedioCompetenciaDto> porCompetencia = new List<PromedioCompetenciaDto>();
        foreach (CompetenciaEvaluacion competencia in competencias)
        {
            List<int> notas = evaluaciones
                .SelectMany(evaluacion => evaluacion.CalificacionesEvaluacion)
                .Where(calificacion => calificacion.CompetenciaEvaluacionId == competencia.Id)
                .Select(calificacion => calificacion.Calificacion)
                .ToList();
            decimal promedio = notas.Count == 0 ? 0m : ValidacionEvaluacion.Redondear((decimal)notas.Sum() / notas.Count);
            porCompetencia.Add(new PromedioCompetenciaDto { Competencia = competencia.Nombre, Promedio = promedio });
        }

        return new ResultadosCicloEvaluacionDto
        {
            Evaluadas = evaluaciones.Count,
            PuntajePromedio = puntajes.Count == 0 ? 0m : ValidacionEvaluacion.Redondear(puntajes.Sum() / puntajes.Count),
            Distribucion = distribucion,
            PorCompetencia = porCompetencia
        };
    }

    public async Task<CicloEvaluacionIdDto> CrearAsync(GuardarCicloEvaluacionRequestDto solicitud)
    {
        CicloEvaluacion ciclo = ValidacionEvaluacion.ValidarCiclo(solicitud);
        string codigo = ciclo.Codigo;
        bool existe = await _unidad.Repository<CicloEvaluacion>().AsQueryable().AnyAsync(otro => otro.Codigo == codigo);
        if (existe)
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        try
        {
            await _unidad.Repository<CicloEvaluacion>().AddAsync(ciclo);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }

    public async Task<CicloEvaluacionIdDto> ActualizarAsync(int id, GuardarCicloEvaluacionRequestDto solicitud)
    {
        CicloEvaluacion nuevo = ValidacionEvaluacion.ValidarCiclo(solicitud);
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, id);
        if (ciclo.Estado != ValidacionEvaluacion.CicloConfiguracion)
        {
            throw ValidacionEvaluacion.CicloNoEnConfiguracion(ciclo.Codigo);
        }

        string codigo = nuevo.Codigo;
        bool existe = await _unidad.Repository<CicloEvaluacion>().AsQueryable()
            .AnyAsync(otro => otro.Codigo == codigo && otro.Id != id);
        if (existe)
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        ciclo.Codigo = nuevo.Codigo;
        ciclo.Nombre = nuevo.Nombre;
        ciclo.Periodo = nuevo.Periodo;
        ciclo.FechaInicioPeriodo = nuevo.FechaInicioPeriodo;
        ciclo.FechaFinPeriodo = nuevo.FechaFinPeriodo;
        ciclo.FechaApertura = nuevo.FechaApertura;
        ciclo.FechaCierre = nuevo.FechaCierre;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCodigoDuplicado(excepcion))
        {
            throw ValidacionEvaluacion.CodigoDuplicado(codigo);
        }

        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }

    public async Task<CompetenciaEvaluacionIdDto> CrearCompetenciaAsync(int cicloId, GuardarCompetenciaEvaluacionRequestDto solicitud)
    {
        CompetenciaEvaluacion competencia = ValidacionEvaluacion.ValidarCompetencia(solicitud);
        CicloEvaluacion ciclo = await ObtenerEnConfiguracionAsync(cicloId);
        await ValidarNombreLibreAsync(cicloId, competencia.Nombre, null);
        competencia.CicloEvaluacionId = ciclo.Id;
        try
        {
            await _unidad.Repository<CompetenciaEvaluacion>().AddAsync(competencia);
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCompetenciaDuplicada(excepcion))
        {
            throw ValidacionEvaluacion.CompetenciaDuplicada(competencia.Nombre);
        }

        return new CompetenciaEvaluacionIdDto { Id = competencia.Id };
    }

    public async Task<CompetenciaEvaluacionIdDto> ActualizarCompetenciaAsync(int cicloId, int competenciaId, GuardarCompetenciaEvaluacionRequestDto solicitud)
    {
        CompetenciaEvaluacion nueva = ValidacionEvaluacion.ValidarCompetencia(solicitud);
        await ObtenerEnConfiguracionAsync(cicloId);
        CompetenciaEvaluacion competencia = await ObtenerCompetenciaAsync(cicloId, competenciaId);
        await ValidarNombreLibreAsync(cicloId, nueva.Nombre, competenciaId);
        competencia.Nombre = nueva.Nombre;
        competencia.Descripcion = nueva.Descripcion;
        competencia.Peso = nueva.Peso;
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (ValidacionEvaluacion.EsCompetenciaDuplicada(excepcion))
        {
            throw ValidacionEvaluacion.CompetenciaDuplicada(nueva.Nombre);
        }

        return new CompetenciaEvaluacionIdDto { Id = competencia.Id };
    }

    public async Task EliminarCompetenciaAsync(int cicloId, int competenciaId)
    {
        await ObtenerEnConfiguracionAsync(cicloId);
        CompetenciaEvaluacion competencia = await ObtenerCompetenciaAsync(cicloId, competenciaId);
        _unidad.Repository<CompetenciaEvaluacion>().Remove(competencia);
        await _unidad.CompleteAsync();
    }

    public async Task<CicloEvaluacionIdDto> AbrirAsync(int id)
    {
        CicloEvaluacion ciclo = await ObtenerEnConfiguracionAsync(id);
        decimal suma = await ValidacionEvaluacion.SumarPesosAsync(_unidad, id);
        if (suma != ValidacionEvaluacion.PesoTotal)
        {
            throw ValidacionEvaluacion.Error409(
                $"Los pesos de las competencias suman {ValidacionEvaluacion.Decimales(suma)} y deben sumar 100.");
        }

        ciclo.Estado = ValidacionEvaluacion.CicloAbierto;
        await _unidad.CompleteAsync();
        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }

    // Cierra el ciclo y sus evaluaciones enviadas; las que siguen en borrador quedan congeladas.
    public async Task<CicloEvaluacionIdDto> CerrarAsync(int id)
    {
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, id);
        if (ciclo.Estado == ValidacionEvaluacion.CicloCerrado)
        {
            throw ValidacionEvaluacion.Error409($"El ciclo {ciclo.Codigo} ya está cerrado.");
        }

        ValidacionEvaluacion.ExigirCicloAbierto(ciclo);
        List<Evaluacion> enviadas = await _unidad.Repository<Evaluacion>().AsQueryable()
            .Where(evaluacion => evaluacion.CicloEvaluacionId == id
                && evaluacion.Estado == ValidacionEvaluacion.EvaluacionEnviada)
            .ToListAsync();
        DateTime ahora = DateTime.UtcNow;
        using TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        foreach (Evaluacion evaluacion in enviadas)
        {
            evaluacion.Estado = ValidacionEvaluacion.EvaluacionCerrada;
            evaluacion.FechaCierre = ahora;
        }

        ciclo.Estado = ValidacionEvaluacion.CicloCerrado;
        await _unidad.CompleteAsync();
        transaccion.Complete();
        return new CicloEvaluacionIdDto { Id = ciclo.Id };
    }

    private async Task<CicloEvaluacion> ObtenerSinSeguimientoAsync(int id)
    {
        CicloEvaluacion? ciclo = await _unidad.Repository<CicloEvaluacion>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (ciclo is null)
        {
            throw ValidacionEvaluacion.CicloInexistente(id);
        }

        return ciclo;
    }

    private async Task<CicloEvaluacion> ObtenerEnConfiguracionAsync(int id)
    {
        CicloEvaluacion ciclo = await ValidacionEvaluacion.ObtenerCicloAsync(_unidad, id);
        if (ciclo.Estado != ValidacionEvaluacion.CicloConfiguracion)
        {
            throw ValidacionEvaluacion.CicloNoEnConfiguracion(ciclo.Codigo);
        }

        return ciclo;
    }

    private async Task<CompetenciaEvaluacion> ObtenerCompetenciaAsync(int cicloId, int competenciaId)
    {
        CompetenciaEvaluacion? competencia = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == competenciaId && entidad.CicloEvaluacionId == cicloId);
        if (competencia is null)
        {
            throw ValidacionEvaluacion.Error404($"La competencia {competenciaId} no existe en el ciclo.");
        }

        return competencia;
    }

    private async Task ValidarNombreLibreAsync(int cicloId, string nombre, int? excluirId)
    {
        string minuscula = nombre.ToLowerInvariant();
        bool existe = await _unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .AnyAsync(otra => otra.CicloEvaluacionId == cicloId
                && otra.Nombre.ToLower() == minuscula
                && (excluirId == null || otra.Id != excluirId.Value));
        if (existe)
        {
            throw ValidacionEvaluacion.CompetenciaDuplicada(nombre);
        }
    }
}
