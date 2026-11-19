using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class ResumenAcuerdosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenAcuerdosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenAcuerdosDto> ExecuteAsync()
    {
        IQueryable<AcuerdoAbastecimiento> acuerdos = _unitOfWork.Repository<AcuerdoAbastecimiento>().AsQueryable();
        int totalAcuerdos = await acuerdos.CountAsync();
        decimal montoMaximoVigente = await acuerdos
            .Where(acuerdo => acuerdo.Estado == ValidacionAcuerdoAbastecimiento.Vigente)
            .SumAsync(acuerdo => (decimal?)acuerdo.MontoMaximo) ?? 0m;
        decimal montoConsumido = await _unitOfWork.Repository<LineaAcuerdoAbastecimiento>()
            .AsQueryable()
            .SumAsync(linea => (decimal?)(linea.PrecioAcordado * linea.CantidadConsumida)) ?? 0m;

        List<EstadoConteoDto> porEstado = new List<EstadoConteoDto>();
        foreach (string estado in ValidacionAcuerdoAbastecimiento.Estados)
        {
            int total = await acuerdos.CountAsync(acuerdo => acuerdo.Estado == estado);
            porEstado.Add(new EstadoConteoDto { Estado = estado, Total = total });
        }

        return new ResumenAcuerdosDto
        {
            TotalAcuerdos = totalAcuerdos,
            MontoMaximoVigente = montoMaximoVigente,
            MontoConsumido = montoConsumido,
            PorEstado = porEstado
        };
    }
}
