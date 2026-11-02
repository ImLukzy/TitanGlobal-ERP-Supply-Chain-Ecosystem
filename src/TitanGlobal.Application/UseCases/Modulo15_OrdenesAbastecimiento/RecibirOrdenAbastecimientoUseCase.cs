using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento;

public class RecibirOrdenAbastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RecibirOrdenAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoDetalleDto> ExecuteAsync(int id)
    {
        OrdenAbastecimiento orden = await ObtenerOrdenAsync(id);
        ActualizarOrdenAbastecimientoUseCase.ExigirEmitida(orden);
        orden.Estado = "Recibida";
        orden.FechaRecepcion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<OrdenAbastecimiento>().Update(orden);
        await _unitOfWork.CompleteAsync();

        return await ProyeccionOrdenAbastecimiento.ObtenerDetalleAsync(id, _unitOfWork);
    }

    private async Task<OrdenAbastecimiento> ObtenerOrdenAsync(int id)
    {
        OrdenAbastecimiento? orden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (orden is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La orden de abastecimiento {id} no existe.");
        }

        return orden;
    }

}
