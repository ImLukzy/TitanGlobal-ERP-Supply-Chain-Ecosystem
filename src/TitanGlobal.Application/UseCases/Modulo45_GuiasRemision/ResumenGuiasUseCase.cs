using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class ResumenGuiasUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenGuiasUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenGuiasDto> ExecuteAsync()
    {
        ResumenGuiasDto? resumen = await _unidad.Repository<GuiaRemision>().AsQueryable()
            .GroupBy(g => 1).Select(grupo => new ResumenGuiasDto
            {
                Total = grupo.Count(),
                Borradores = grupo.Count(g => g.Estado == "Borrador"),
                Emitidas = grupo.Count(g => g.Estado == "Emitida"),
                Anuladas = grupo.Count(g => g.Estado == "Anulada")
            }).FirstOrDefaultAsync();
        return resumen ?? new ResumenGuiasDto();
    }
}
