using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento;

public class AnularOrdenAbastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AnularOrdenAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoDetalleDto> ExecuteAsync(int id, AnularOrdenRequestDto solicitud)
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

        ActualizarOrdenAbastecimientoUseCase.ExigirEmitida(orden);
        string motivo = (solicitud.Motivo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de anulación es obligatorio.");
        }

        if (motivo.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de anulación no puede superar 500 caracteres.");
        }

        orden.Estado = "Anulada";
        orden.MotivoAnulacion = motivo;
        _unitOfWork.Repository<OrdenAbastecimiento>().Update(orden);
        await _unitOfWork.CompleteAsync();

        return await ProyeccionOrdenAbastecimiento.ObtenerDetalleAsync(id, _unitOfWork);
    }
}
