using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class ResumenRetornosUseCase
{
    private readonly IUnitOfWork _unidad;
    public ResumenRetornosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<ResumenRetornosDto> ExecuteAsync()
    {
        ResumenRetornosDto? resumen = await _unidad.Repository<RetornoLogistico>().AsQueryable().GroupBy(r => 1).Select(g => new ResumenRetornosDto
        {
            Total = g.Count(),
            Programados = g.Count(r => r.Estado == "Programado"),
            Recogidos = g.Count(r => r.Estado == "Recogido"),
            Recibidos = g.Count(r => r.Estado == "Recibido"),
            Cancelados = g.Count(r => r.Estado == "Cancelado")
        }).FirstOrDefaultAsync();
        return resumen ?? new ResumenRetornosDto();
    }
}
