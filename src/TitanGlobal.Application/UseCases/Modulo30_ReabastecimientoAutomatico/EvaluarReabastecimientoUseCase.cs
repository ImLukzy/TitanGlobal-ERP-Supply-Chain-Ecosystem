using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class EvaluarReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EvaluarReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResultadoEvaluacionReabastecimientoDto> ExecuteAsync(
        EvaluarReabastecimientoRequestDto? solicitud)
    {
        IQueryable<ReglaReabastecimiento> consulta = _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .Include(registro => registro.Producto)
            .Include(registro => registro.Almacen)
            .Include(registro => registro.ProveedorPreferido)
            .Include(registro => registro.EjecucionesReabastecimiento)
            .Where(registro => registro.Activa);

        if (solicitud?.AlmacenId.HasValue == true)
        {
            consulta = consulta.Where(registro => registro.AlmacenId == solicitud.AlmacenId.Value);
        }

        if (solicitud?.ReglaId.HasValue == true)
        {
            consulta = consulta.Where(registro => registro.Id == solicitud.ReglaId.Value);
        }

        List<ReglaReabastecimiento> reglas = await consulta
            .OrderBy(registro => registro.Codigo)
            .ThenBy(registro => registro.Id)
            .ToListAsync();

        DateTime ahora = DateTime.UtcNow;
        int bajoPunto = 0;
        int generadas = 0;
        int actualizadas = 0;
        List<EjecucionReabastecimiento> procesadas = new List<EjecucionReabastecimiento>();

        foreach (ReglaReabastecimiento regla in reglas)
        {
            regla.UltimaEvaluacion = ahora;

            if (regla.StockActual <= regla.PuntoReorden)
            {
                bajoPunto++;
                int cantidadSugerida = ValidacionReabastecimiento.CalcularCantidadSugerida(regla);

                EjecucionReabastecimiento? pendiente = regla.EjecucionesReabastecimiento
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefault(item => item.Estado == "Pendiente");

                if (pendiente is not null)
                {
                    pendiente.FechaEjecucion = ahora;
                    pendiente.StockActual = regla.StockActual;
                    pendiente.CantidadSugerida = cantidadSugerida;
                    pendiente.Observacion = $"Sugerencia actualizada por evaluación automática (stock {regla.StockActual} <= punto de reorden {regla.PuntoReorden}).";
                    actualizadas++;
                    procesadas.Add(pendiente);
                }
                else
                {
                    EjecucionReabastecimiento nueva = new EjecucionReabastecimiento
                    {
                        ReglaReabastecimientoId = regla.Id,
                        ReglaReabastecimiento = regla,
                        FechaEjecucion = ahora,
                        StockActual = regla.StockActual,
                        CantidadSugerida = cantidadSugerida,
                        Estado = "Pendiente",
                        Observacion = $"Stock actual ({regla.StockActual}) en o por debajo del punto de reorden ({regla.PuntoReorden})."
                    };
                    await _unitOfWork.Repository<EjecucionReabastecimiento>().AddAsync(nueva);
                    generadas++;
                    procesadas.Add(nueva);
                }
            }
        }

        await _unitOfWork.CompleteAsync();

        List<EjecucionReabastecimientoItemDto> itemsDto = procesadas
            .Select(ValidacionReabastecimiento.MapearEjecucion)
            .ToList();

        return new ResultadoEvaluacionReabastecimientoDto
        {
            FechaEvaluacion = ahora,
            ReglasEvaluadas = reglas.Count,
            ReglasBajoPuntoReorden = bajoPunto,
            SugerenciasGeneradas = generadas,
            SugerenciasActualizadas = actualizadas,
            Items = itemsDto
        };
    }
}
