using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

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
            throw new TitanGlobal.Api.Middlewares.ReglaNegocioException(
                StatusCodes.Status404NotFound,
                $"La orden de abastecimiento {id} no existe.");
        }

        ActualizarOrdenAbastecimientoUseCase.ExigirEmitida(orden);
        string motivo = (solicitud.Motivo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new TitanGlobal.Api.Middlewares.ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El motivo de anulación es obligatorio.");
        }

        if (motivo.Length > 500)
        {
            throw new TitanGlobal.Api.Middlewares.ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El motivo de anulación no puede superar 500 caracteres.");
        }

        orden.Estado = "Anulada";
        orden.MotivoAnulacion = motivo;
        _unitOfWork.Repository<OrdenAbastecimiento>().Update(orden);
        await _unitOfWork.CompleteAsync();

        return await ProyeccionOrdenAbastecimiento.ObtenerDetalleAsync(id, _unitOfWork);
    }
}
