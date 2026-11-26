using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

public class ResumenTomasInventarioUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenTomasInventarioUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenTomasInventarioDto> ExecuteAsync()
    {
        List<TomaInventario> tomas = await _unitOfWork.Repository<TomaInventario>()
            .AsQueryable()
            .AsNoTracking()
            .Include(registro => registro.DetallesTomaInventario)
            .ToListAsync();

        return new ResumenTomasInventarioDto
        {
            TotalTomas = tomas.Count,
            Abiertas = tomas.Count(registro => registro.Estado == "Abierta"),
            Cerradas = tomas.Count(registro => registro.Estado == "Cerrada"),
            Anuladas = tomas.Count(registro => registro.Estado == "Anulada"),
            LineasConDiferencia = tomas.Sum(registro =>
                registro.DetallesTomaInventario.Count(detalle => detalle.Diferencia != 0))
        };
    }
}
