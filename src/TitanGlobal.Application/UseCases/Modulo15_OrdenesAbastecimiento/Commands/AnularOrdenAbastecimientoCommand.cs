using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Commands;

public class AnularOrdenAbastecimientoCommand : IRequest<OrdenAbastecimientoDetalleDto>
{
    public int Id { get; set; }

    public required AnularOrdenRequestDto Solicitud { get; set; }
}

public class AnularOrdenAbastecimientoCommandHandler : IRequestHandler<AnularOrdenAbastecimientoCommand, OrdenAbastecimientoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AnularOrdenAbastecimientoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoDetalleDto> Handle(AnularOrdenAbastecimientoCommand request, CancellationToken cancellationToken)
    {
        OrdenAbastecimiento? orden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (orden is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La orden de abastecimiento {request.Id} no existe.");
        }

        OperacionesOrdenAbastecimiento.ExigirEmitida(orden);
        string motivo = (request.Solicitud.Motivo ?? string.Empty).Trim();
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

        return await ProyeccionOrdenAbastecimiento.ObtenerDetalleAsync(request.Id, _unitOfWork);
    }
}
