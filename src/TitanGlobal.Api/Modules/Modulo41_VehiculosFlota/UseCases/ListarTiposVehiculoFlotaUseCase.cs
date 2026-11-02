using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

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
