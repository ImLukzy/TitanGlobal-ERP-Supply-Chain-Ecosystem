using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo12_EvaluacionProveedores;

public class ResumenEvaluacionesProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenEvaluacionesProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenEvaluacionesDto> ExecuteAsync()
    {
        // Se traen solo las columnas necesarias y los promedios se calculan en memoria,
        // para redondear igual en PostgreSQL y en la base de pruebas.
        List<FilaEvaluacion> filas = await _unitOfWork.Repository<EvaluacionProveedor>()
            .AsQueryable()
            .Select(evaluacion => new FilaEvaluacion
            {
                ProveedorId = evaluacion.ProveedorId,
                ProveedorNombre = evaluacion.Proveedor.RazonSocial,
                Periodo = evaluacion.Periodo,
                PuntajeFinal = evaluacion.PuntajeFinal,
                Calificacion = evaluacion.Calificacion
            })
            .ToListAsync();

        decimal promedioGeneral = filas.Count == 0
            ? 0m
            : Math.Round(filas.Average(fila => fila.PuntajeFinal), 2, MidpointRounding.AwayFromZero);

        List<CalificacionConteoDto> porCalificacion = ValidacionEvaluacionProveedor.Calificaciones
            .Select(nombre => new CalificacionConteoDto
            {
                Calificacion = nombre,
                Total = filas.Count(fila => fila.Calificacion == nombre)
            })
            .ToList();

        List<RankingProveedorDto> ranking = filas
            .GroupBy(fila => new { fila.ProveedorId, fila.ProveedorNombre })
            .Select(grupo => new RankingProveedorDto
            {
                ProveedorId = grupo.Key.ProveedorId,
                ProveedorNombre = grupo.Key.ProveedorNombre,
                Evaluaciones = grupo.Count(),
                Promedio = Math.Round(grupo.Average(fila => fila.PuntajeFinal), 2, MidpointRounding.AwayFromZero),
                UltimaCalificacion = grupo.OrderByDescending(fila => fila.Periodo).First().Calificacion
            })
            .OrderByDescending(proveedor => proveedor.Promedio)
            .ThenBy(proveedor => proveedor.ProveedorNombre, StringComparer.Ordinal)
            .ToList();

        return new ResumenEvaluacionesDto
        {
            TotalEvaluaciones = filas.Count,
            ProveedoresEvaluados = ranking.Count,
            PromedioGeneral = promedioGeneral,
            PorCalificacion = porCalificacion,
            Ranking = ranking
        };
    }

    private sealed class FilaEvaluacion
    {
        public int ProveedorId { get; set; }

        public string ProveedorNombre { get; set; } = string.Empty;

        public string Periodo { get; set; } = string.Empty;

        public decimal PuntajeFinal { get; set; }

        public string Calificacion { get; set; } = string.Empty;
    }
}
