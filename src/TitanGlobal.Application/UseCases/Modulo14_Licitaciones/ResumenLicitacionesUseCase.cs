using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class ResumenLicitacionesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenLicitacionesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenLicitacionesDto> ExecuteAsync()
    {
        IQueryable<Licitacion> licitaciones = _unitOfWork.Repository<Licitacion>().AsQueryable();
        int totalLicitaciones = await licitaciones.CountAsync();
        int totalOfertas = await _unitOfWork.Repository<OfertaLicitacion>().AsQueryable().CountAsync();
        decimal presupuestoAbierto = await licitaciones
            .Where(licitacion => licitacion.Estado == ValidacionLicitacion.Abierta)
            .SumAsync(licitacion => (decimal?)licitacion.PresupuestoReferencial) ?? 0m;
        decimal montoAdjudicado = await licitaciones
            .SumAsync(licitacion => licitacion.MontoAdjudicado) ?? 0m;

        List<EstadoConteoDto> porEstado = new List<EstadoConteoDto>();
        foreach (string estado in ValidacionLicitacion.Estados)
        {
            int total = await licitaciones.CountAsync(licitacion => licitacion.Estado == estado);
            porEstado.Add(new EstadoConteoDto { Estado = estado, Total = total });
        }

        return new ResumenLicitacionesDto
        {
            TotalLicitaciones = totalLicitaciones,
            TotalOfertas = totalOfertas,
            PresupuestoAbierto = presupuestoAbierto,
            MontoAdjudicado = montoAdjudicado,
            PorEstado = porEstado
        };
    }
}
