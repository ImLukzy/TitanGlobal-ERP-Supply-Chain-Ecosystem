using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo17_AgentesAduaneros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo17_AgentesAduaneros;

public class ResumenDeclaracionesAduanaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenDeclaracionesAduanaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenDeclaracionesDto> ExecuteAsync()
    {
        IQueryable<DeclaracionAduana> declaraciones = _unitOfWork.Repository<DeclaracionAduana>().AsQueryable();
        int total = await declaraciones.CountAsync();
        decimal derechosVigentes = await declaraciones
            .Where(declaracion => declaracion.Estado != ValidacionDeclaracionAduana.Anulada)
            .SumAsync(declaracion => declaracion.DerechosArancel);

        List<EstadoDeclaracionConteoDto> porEstado = new List<EstadoDeclaracionConteoDto>();
        foreach (string estado in ValidacionDeclaracionAduana.Estados)
        {
            int cantidad = await declaraciones.CountAsync(declaracion => declaracion.Estado == estado);
            porEstado.Add(new EstadoDeclaracionConteoDto { Estado = estado, Total = cantidad });
        }

        return new ResumenDeclaracionesDto
        {
            TotalDeclaraciones = total,
            DerechosArancelVigentes = derechosVigentes,
            PorEstado = porEstado
        };
    }
}
