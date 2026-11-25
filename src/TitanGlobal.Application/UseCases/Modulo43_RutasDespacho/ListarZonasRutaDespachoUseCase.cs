using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

public class ListarZonasRutaDespachoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarZonasRutaDespachoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<string>> ExecuteAsync()
    {
        List<string> zonas = await _unitOfWork.Repository<RutaDespacho>().AsQueryable().Select(r => r.Zona).Distinct().OrderBy(z => z).ToListAsync();
        return zonas;
    }
}
