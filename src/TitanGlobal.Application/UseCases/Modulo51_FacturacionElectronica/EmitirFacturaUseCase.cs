using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class EmitirFacturaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EmitirFacturaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FacturaDetalleDto> ExecuteAsync(int id)
    {
        IGenericRepository<Factura> repositorio =
            _unitOfWork.Repository<Factura>();

        Factura? factura = await repositorio
            .AsQueryable()
            .Include(factura => factura.DetallesFactura)
            .FirstOrDefaultAsync(factura => factura.Id == id);

        if (factura == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "La factura solicitada no existe.");
        }

        if (factura.Estado != ValidacionFactura.EstadoBorrador)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Solo se pueden emitir facturas en estado Borrador.");
        }

        if (factura.DetallesFactura.Count == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "No se puede emitir una factura sin productos.");
        }

        if (factura.Total <= 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El total de la factura debe ser mayor que cero para emitirla.");
        }

        decimal subtotal = factura.DetallesFactura
            .Sum(detalle => detalle.Subtotal);

        decimal descuento = factura.DetallesFactura
            .Sum(detalle => detalle.ImporteDescuento);

        decimal impuesto = factura.DetallesFactura
            .Sum(detalle => detalle.ImporteImpuesto);

        decimal total = factura.DetallesFactura
            .Sum(detalle => detalle.Total);

        if (factura.Subtotal != subtotal ||
            factura.TotalDescuento != descuento ||
            factura.TotalImpuesto != impuesto ||
            factura.Total != total)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Los importes de la factura no coinciden con sus detalles.");
        }

        factura.Estado = ValidacionFactura.EstadoEmitida;

        repositorio.Update(factura);

        await _unitOfWork.CompleteAsync();

        ObtenerFacturaUseCase obtenerFactura =
            new ObtenerFacturaUseCase(_unitOfWork);

        return await obtenerFactura.ExecuteAsync(factura.Id);
    }
}
