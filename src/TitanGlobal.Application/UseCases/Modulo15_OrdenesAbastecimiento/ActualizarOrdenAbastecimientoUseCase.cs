using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo15_OrdenesAbastecimiento;

public class ActualizarOrdenAbastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarOrdenAbastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrdenAbastecimientoCreadaDto> ExecuteAsync(
        int id,
        GuardarOrdenAbastecimientoRequestDto solicitud)
    {
        GuardarOrdenAbastecimientoRequestDto datosValidados = await ValidacionOrdenAbastecimiento.ValidarAsync(
            solicitud,
            _unitOfWork);
        OrdenAbastecimiento? orden = await _unitOfWork.Repository<OrdenAbastecimiento>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (orden is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La orden de abastecimiento {id} no existe.");
        }

        ExigirEmitida(orden);
        List<DetalleOrdenAbastecimiento> detallesAnteriores = await _unitOfWork
            .Repository<DetalleOrdenAbastecimiento>()
            .AsQueryable()
            .Where(detalle => detalle.OrdenAbastecimientoId == id)
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
                OrdenAbastecimientoId = id,
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

    internal static void ExigirEmitida(OrdenAbastecimiento orden)
    {
        if (orden.Estado != "Emitida")
        {
            string estadoNormalizado = orden.Estado.ToLowerInvariant();
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La orden {orden.Numero} ya está {estadoNormalizado}.");
        }
    }
}
