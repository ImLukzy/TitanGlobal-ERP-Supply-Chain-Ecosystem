using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

public class ResumenRutasDespachoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenRutasDespachoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenRutasDespachoDto> ExecuteAsync()
    {
        List<ZonaRutasDespachoDto> zonas = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().GroupBy(r => r.Zona)
            .Select(grupo => new ZonaRutasDespachoDto
            {
                Zona = grupo.Key,
                TotalRutas = grupo.Count(),
                Activas = grupo.Count(r => r.Activo)
            }).OrderByDescending(z => z.TotalRutas).ThenBy(z => z.Zona).ToListAsync();
        int total = zonas.Sum(z => z.TotalRutas);
        int activas = zonas.Sum(z => z.Activas);
        return new ResumenRutasDespachoDto { TotalRutas = total, Activas = activas, Inactivas = total - activas, PorZona = zonas };
    }
}
