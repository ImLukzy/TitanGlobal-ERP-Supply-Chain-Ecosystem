using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

public class ResumenSucursalesUseCase
{
    private readonly IUnitOfWork _unidad;
    public ResumenSucursalesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<ResumenSucursalesDto> ExecuteAsync()
    {
        ResumenSucursalesDto? resumen = await _unidad.Repository<Sucursal>().AsQueryable().GroupBy(s => 1).Select(g => new ResumenSucursalesDto
        {
            Total = g.Count(),
            Activas = g.Count(s => s.Activa),
            Inactivas = g.Count(s => !s.Activa),
            Filiales = g.Count(s => s.Tipo == "Filial"),
            Plantas = g.Count(s => s.Tipo == "Planta"),
            Oficinas = g.Count(s => s.Tipo == "Oficina")
        }).FirstOrDefaultAsync();
        return resumen ?? new ResumenSucursalesDto();
    }
}
