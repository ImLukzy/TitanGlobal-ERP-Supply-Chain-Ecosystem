using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento;

public class ObtenerOrdenAbastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerOrdenAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoDetalleDto> ExecuteAsync(int id)
    {
        DateTime hoyUtc = DateTime.UtcNow.Date;
        OrdenAbastecimientoDetalleDto? orden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            // PATRÓN LINQ 10: proyección jerárquica de la orden y sus líneas.
            .Select(ProyeccionOrdenAbastecimiento.CrearSelector(hoyUtc))
            .SingleOrDefaultAsync();
        if (orden is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La orden de abastecimiento {id} no existe.");
        }

        return orden;
    }
}
