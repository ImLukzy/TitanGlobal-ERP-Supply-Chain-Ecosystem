using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class ResumenLotesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenLotesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenLotesDto> ExecuteAsync()
    {
        IQueryable<Lote> consulta = _unitOfWork.Repository<Lote>().AsQueryable();
        DateTime hoy = DateTime.UtcNow.Date;
        DateTime limite = hoy.AddDays(30);

        int total = await consulta.CountAsync();
        int disponibles = await consulta.CountAsync(registro => registro.Estado == "Disponible");
        int enCuarentena = await consulta.CountAsync(registro => registro.Estado == "Cuarentena");
        int bloqueados = await consulta.CountAsync(registro => registro.Estado == "Bloqueado");
        int vencidos = await consulta.CountAsync(
            registro => registro.FechaVencimiento != null && registro.FechaVencimiento < hoy);
        int porVencer = await consulta.CountAsync(
            registro => registro.FechaVencimiento != null
                        && registro.FechaVencimiento >= hoy
                        && registro.FechaVencimiento <= limite);

        return new ResumenLotesDto
        {
            TotalLotes = total,
            Disponibles = disponibles,
            EnCuarentena = enCuarentena,
            Bloqueados = bloqueados,
            Vencidos = vencidos,
            PorVencer = porVencer
        };
    }
}
