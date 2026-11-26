using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo27_TransferenciasAlmacen;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo27_TransferenciasAlmacen;

public class ResumenTransferenciasUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenTransferenciasUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenTransferenciasDto> ExecuteAsync()
    {
        IQueryable<TransferenciaAlmacen> consulta = _unitOfWork.Repository<TransferenciaAlmacen>().AsQueryable();

        int total = await consulta.CountAsync();
        int unidades = await consulta.SumAsync(registro => (int?)registro.Cantidad) ?? 0;
        decimal valor = await consulta.SumAsync(registro => (decimal?)(registro.Cantidad * registro.CostoUnitario)) ?? 0m;

        return new ResumenTransferenciasDto
        {
            TotalTransferencias = total,
            UnidadesTransferidas = unidades,
            ValorTransferido = Math.Round(valor, 2)
        };
    }
}
