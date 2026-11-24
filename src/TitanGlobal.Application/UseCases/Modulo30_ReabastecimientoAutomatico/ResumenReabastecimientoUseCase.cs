using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class ResumenReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenReabastecimientoDto> ExecuteAsync()
    {
        List<ReglaReabastecimiento> reglas = await _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .AsNoTracking()
            .ToListAsync();

        List<EjecucionReabastecimiento> ejecuciones = await _unitOfWork.Repository<EjecucionReabastecimiento>()
            .AsQueryable()
            .AsNoTracking()
            .ToListAsync();

        int totalReglas = reglas.Count;
        int reglasActivas = reglas.Count(registro => registro.Activa);
        int reglasEnAlerta = reglas.Count(registro => registro.Activa && registro.StockActual <= registro.PuntoReorden);

        List<EjecucionReabastecimiento> pendientes = ejecuciones
            .Where(registro => registro.Estado == "Pendiente")
            .ToList();

        int solicitudesGeneradas = ejecuciones.Count(registro => registro.Estado == "SolicitudGenerada");
        int unidadesPendientes = pendientes.Sum(registro => registro.CantidadSugerida);

        return new ResumenReabastecimientoDto
        {
            TotalReglas = totalReglas,
            ReglasActivas = reglasActivas,
            ReglasEnAlerta = reglasEnAlerta,
            SugerenciasPendientes = pendientes.Count,
            SolicitudesGeneradas = solicitudesGeneradas,
            UnidadesSugeridasPendientes = unidadesPendientes
        };
    }
}
