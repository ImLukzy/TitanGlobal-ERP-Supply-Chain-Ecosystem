using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo;

public class ResumenCentrosTrabajoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenCentrosTrabajoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenCentrosTrabajoDto> ExecuteAsync()
    {
        IQueryable<CentroTrabajo> centros = _unitOfWork.Repository<CentroTrabajo>().AsQueryable();
        int totalCentros = await centros.CountAsync();
        int activos = await centros.CountAsync(centro => centro.Activo);
        decimal capacidadTotalHorasDia = await centros.SumAsync(centro => centro.CapacidadHorasDia);
        decimal capacidadActivaHorasDia = await centros
            .Where(centro => centro.Activo)
            .SumAsync(centro => centro.CapacidadHorasDia);
        // PATRÓN LINQ 9: agrupación con orden explícito.
        List<TipoCentrosTrabajoDto> porTipo = await centros
            .GroupBy(centro => centro.Tipo)
            .Select(grupo => new TipoCentrosTrabajoDto
            {
                Tipo = grupo.Key,
                TotalCentros = grupo.Count(),
                CapacidadHorasDia = grupo.Sum(centro => centro.CapacidadHorasDia)
            })
            .OrderBy(tipo => tipo.Tipo == "Linea" ? 0 : tipo.Tipo == "Celda" ? 1 : 2)
            .ToListAsync();
        List<PlantaCentrosTrabajoDto> porPlanta = await centros
            .GroupBy(centro => centro.Planta)
            .Select(grupo => new PlantaCentrosTrabajoDto
            {
                Planta = grupo.Key,
                TotalCentros = grupo.Count(),
                Activos = grupo.Count(centro => centro.Activo)
            })
            .OrderByDescending(planta => planta.TotalCentros)
            .ThenBy(planta => planta.Planta)
            .ToListAsync();

        return new ResumenCentrosTrabajoDto
        {
            TotalCentros = totalCentros,
            Activos = activos,
            Inactivos = totalCentros - activos,
            CapacidadTotalHorasDia = capacidadTotalHorasDia,
            CapacidadActivaHorasDia = capacidadActivaHorasDia,
            PorTipo = porTipo,
            PorPlanta = porPlanta
        };
    }
}
