using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo26_KardexValorizado;

public class ResumenKardexUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenKardexUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenKardexDto> ExecuteAsync()
    {
        IQueryable<MovimientoKardex> consulta = _unitOfWork.Repository<MovimientoKardex>().AsQueryable();

        int total = await consulta.CountAsync();
        int totalEntradas = await consulta.CountAsync(registro => registro.Tipo == "Entrada");
        int totalSalidas = await consulta.CountAsync(registro => registro.Tipo == "Salida");
        int unidadesEntradas = await consulta
            .Where(registro => registro.Tipo == "Entrada")
            .SumAsync(registro => (int?)registro.Cantidad) ?? 0;
        int unidadesSalidas = await consulta
            .Where(registro => registro.Tipo == "Salida")
            .SumAsync(registro => (int?)registro.Cantidad) ?? 0;

        return new ResumenKardexDto
        {
            TotalMovimientos = total,
            TotalEntradas = totalEntradas,
            TotalSalidas = totalSalidas,
            UnidadesEntradas = unidadesEntradas,
            UnidadesSalidas = unidadesSalidas
        };
    }
}
