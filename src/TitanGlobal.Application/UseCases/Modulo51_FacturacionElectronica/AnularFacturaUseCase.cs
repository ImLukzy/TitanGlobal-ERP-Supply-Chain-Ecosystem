using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class AnularFacturaUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;

    public AnularFacturaUseCase(IUnitOfWorkFacturas unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<FacturaDetalleDto> ExecuteAsync(
        int id,
        AnularFacturaRequestDto request)
    {
        return _unitOfWork.ConFacturaBloqueadaAsync(id, AnularBajoBloqueoAsync);

        Task<FacturaDetalleDto> AnularBajoBloqueoAsync()
        {
            return AnularAsync(id, request);
        }
    }

    private async Task<FacturaDetalleDto> AnularAsync(int id, AnularFacturaRequestDto request)
    {
        IGenericRepository<Factura> repositorio =
            _unitOfWork.Repository<Factura>();

        Factura? factura = await repositorio
            .AsQueryable()
            .FirstOrDefaultAsync(factura => factura.Id == id);

        if (factura == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "La factura solicitada no existe.");
        }

        if (factura.Estado == ValidacionFactura.EstadoAnulada)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "La factura ya se encuentra anulada.");
        }

        if (factura.Estado != ValidacionFactura.EstadoBorrador &&
            factura.Estado != ValidacionFactura.EstadoEmitida)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "La factura no se encuentra en un estado válido para su anulación.");
        }

        string motivo =
            ValidacionFactura.NormalizarMotivoAnulacion(
                request.Motivo);

        bool tieneNotasEmitidas = await _unitOfWork.Repository<NotaCreditoDebito>()
            .AsQueryable()
            .AnyAsync(nota => nota.FacturaId == id && nota.Estado == NotaCreditoDebito.EstadoEmitida);
        if (tieneNotasEmitidas)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "No se puede anular un comprobante con notas emitidas.");
        }

        factura.Estado = ValidacionFactura.EstadoAnulada;

        factura.FechaAnulacion = DateTime.UtcNow;

        factura.MotivoAnulacion = motivo;

        repositorio.Update(factura);

        await _unitOfWork.CompleteAsync();

        ObtenerFacturaUseCase obtenerFactura =
            new ObtenerFacturaUseCase(_unitOfWork);

        return await obtenerFactura.ExecuteAsync(factura.Id);
    }
}
