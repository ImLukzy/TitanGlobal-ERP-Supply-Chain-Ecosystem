using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo48_TransportistasTerceros;

public class ResumenTransportistasTercerosUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenTransportistasTercerosUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenTransportistasTercerosDto> ExecuteAsync()
    {
        IQueryable<TransportistaTercero> consulta = _unitOfWork.Repository<TransportistaTercero>().AsQueryable();
        int total = await consulta.CountAsync();
        int activos = await consulta.CountAsync(t => t.Activo);
        // PATRÓN LINQ 2: agrupación y agregación en la consulta.
        List<TipoServicioTransportistasDto> tipos = await consulta.GroupBy(t => t.TipoServicio)
            .Select(grupo => new TipoServicioTransportistasDto
            {
                TipoServicio = grupo.Key,
                TotalTransportistas = grupo.Count(),
                Activos = grupo.Count(t => t.Activo)
            }).OrderByDescending(t => t.TotalTransportistas).ThenBy(t => t.TipoServicio).ToListAsync();
        return new ResumenTransportistasTercerosDto { TotalTransportistas = total, Activos = activos, Inactivos = total - activos, PorTipoServicio = tipos };
    }
}
