using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class ResumenSiniestrosUseCase
{
    private readonly IUnitOfWork _unidad;
    public ResumenSiniestrosUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<ResumenSiniestrosDto> ExecuteAsync()
    {
        ResumenSiniestrosDto? resumen = await _unidad.Repository<SiniestroFlota>().AsQueryable().GroupBy(r => 1).Select(g => new ResumenSiniestrosDto
        {
            Total = g.Count(),
            Registrados = g.Count(r => r.Estado == "Registrado"),
            EnReclamo = g.Count(r => r.Estado == "EnReclamo"),
            Cerrados = g.Count(r => r.Estado == "Cerrado"),
            Anulados = g.Count(r => r.Estado == "Anulado")
        }).FirstOrDefaultAsync();
        return resumen ?? new ResumenSiniestrosDto();
    }
}
