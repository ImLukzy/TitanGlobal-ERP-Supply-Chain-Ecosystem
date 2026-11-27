using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento.Commands;

public class ActualizarOrdenAbastecimientoCommand : IRequest<OrdenAbastecimientoCreadaDto>
{
    public int Id { get; set; }

    public required GuardarOrdenAbastecimientoRequestDto Solicitud { get; set; }
}

public class ActualizarOrdenAbastecimientoCommandHandler : IRequestHandler<ActualizarOrdenAbastecimientoCommand, OrdenAbastecimientoCreadaDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarOrdenAbastecimientoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoCreadaDto> Handle(ActualizarOrdenAbastecimientoCommand request, CancellationToken cancellationToken)
    {
        GuardarOrdenAbastecimientoRequestDto datosValidados = await ValidacionOrdenAbastecimiento.ValidarAsync(
            request.Solicitud,
            _unitOfWork);
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
        List<DetalleOrdenAbastecimiento> detallesAnteriores = await _unitOfWork
            .Repository<DetalleOrdenAbastecimiento>()
            .AsQueryable()
            .Where(detalle => detalle.OrdenAbastecimientoId == request.Id)
            .ToListAsync();
        foreach (DetalleOrdenAbastecimiento detalle in detallesAnteriores)
        {
            _unitOfWork.Repository<DetalleOrdenAbastecimiento>().Remove(detalle);
        }

        orden.ProveedorId = datosValidados.ProveedorId;
        orden.FechaEntregaEstimada = datosValidados.FechaEntregaEstimada;
        orden.Observaciones = datosValidados.Observaciones;
        foreach (LineaOrdenAbastecimientoRequestDto linea in datosValidados.Lineas!)
        {
            await _unitOfWork.Repository<DetalleOrdenAbastecimiento>().AddAsync(new DetalleOrdenAbastecimiento
            {
                OrdenAbastecimientoId = request.Id,
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario
            });
        }

        _unitOfWork.Repository<OrdenAbastecimiento>().Update(orden);
        await _unitOfWork.CompleteAsync();

        return new OrdenAbastecimientoCreadaDto
        {
            Id = orden.Id,
            Numero = orden.Numero
        };
    }
}
