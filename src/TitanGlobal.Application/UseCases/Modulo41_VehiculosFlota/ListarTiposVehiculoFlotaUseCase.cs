using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota;

public class ListarTiposVehiculoFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarTiposVehiculoFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
    {
        // PATRÓN LINQ 11: aplanado y únicos (Distinct)
        List<string> tipos = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .Select(v => v.Tipo)
            .Distinct()
            .OrderBy(tipo => tipo)
            .ToListAsync();

        return tipos;
    }
}
