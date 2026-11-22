using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

public class ResumenValesUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenValesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenValesDto> ExecuteAsync()
    {
        ResumenValesDto? resumen = await _unidad.Repository<ValeCombustible>().AsQueryable().GroupBy(v => 1)
            .Select(grupo => new ResumenValesDto
            {
                Total = grupo.Count(),
                Borradores = grupo.Count(v => v.Estado == "Borrador"),
                Confirmados = grupo.Count(v => v.Estado == "Confirmado"),
                Anulados = grupo.Count(v => v.Estado == "Anulado")
            }).FirstOrDefaultAsync();
        return resumen ?? new ResumenValesDto();
    }
}
