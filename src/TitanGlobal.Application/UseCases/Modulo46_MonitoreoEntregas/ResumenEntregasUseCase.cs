using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class ResumenEntregasUseCase
{
    private readonly IUnitOfWork _unidad;

    public ResumenEntregasUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenEntregasDto> ExecuteAsync()
    {
        ResumenEntregasDto? resumen = await _unidad.Repository<EntregaDespacho>().AsQueryable()
            .GroupBy(e => 1).Select(grupo => new ResumenEntregasDto
            {
                Total = grupo.Count(),
                Programadas = grupo.Count(e => e.Estado == "Programada"),
                EnRuta = grupo.Count(e => e.Estado == "EnRuta"),
                Entregadas = grupo.Count(e => e.Estado == "Entregada"),
                Canceladas = grupo.Count(e => e.Estado == "Cancelada")
            }).FirstOrDefaultAsync();
        return resumen ?? new ResumenEntregasDto();
    }
}
