using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class CrearFacturaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearFacturaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FacturaDetalleDto> ExecuteAsync(
        GuardarFacturaRequestDto request)
    {
        ValidacionFactura.ValidarSolicitud(request);

        IGenericRepository<Cliente> repositorioClientes =
            _unitOfWork.Repository<Cliente>();

        IGenericRepository<OrdenCompra> repositorioOrdenes =
            _unitOfWork.Repository<OrdenCompra>();

        IGenericRepository<Factura> repositorioFacturas =
            _unitOfWork.Repository<Factura>();

        Cliente? cliente = await repositorioClientes
            .AsQueryable()
            .FirstOrDefaultAsync(cliente => cliente.Id == request.ClienteId);

        if (cliente == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "El cliente seleccionado no existe.");
        }

        OrdenCompra? ordenCompra = null;

        if (request.OrdenCompraId.HasValue)
        {
            ordenCompra = await repositorioOrdenes
                .AsQueryable()
                .FirstOrDefaultAsync(orden => orden.Id == request.OrdenCompraId.Value);

            if (ordenCompra == null)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status404NotFound,
                    "La orden de compra seleccionada no existe.");
            }

            if (ordenCompra.ClienteId != request.ClienteId)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "La orden de compra no pertenece al cliente seleccionado.");
            }
        }

        bool comprobanteExistente = await repositorioFacturas
            .AsQueryable()
            .AnyAsync(factura =>
                factura.TipoComprobante == request.TipoComprobante &&
                factura.Serie == request.Serie &&
                factura.Numero == request.Numero);

        if (comprobanteExistente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Ya existe un comprobante con la misma serie y número.");
        }

        CalculoFactura calculoFactura =
            new CalculoFactura(_unitOfWork);

        List<DetalleFactura> detalles =
            await calculoFactura.CalcularDetallesAsync(request);

        Factura nuevaFactura = new Factura
        {
            ClienteId = request.ClienteId,
            OrdenCompraId = request.OrdenCompraId,
            TipoComprobante = request.TipoComprobante,
            Serie = request.Serie,
            Numero = request.Numero,
            FechaEmision = request.FechaEmision!.Value,
            FechaVencimiento = request.FechaVencimiento,
            Moneda = request.Moneda,
            Estado = ValidacionFactura.EstadoBorrador,
            Observaciones = request.Observaciones,
            FechaCreacion = DateTime.UtcNow,
            DetallesFactura = detalles
        };

        CalculoFactura.CalcularTotales(nuevaFactura);

        try
        {
            await repositorioFacturas.AddAsync(nuevaFactura);

            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException ex)
            when (ex.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Ya existe un comprobante con la misma serie y número.");
        }
        catch (ViolacionRestriccionException ex)
            when (ex.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "No se pudo guardar la factura porque una referencia ya no existe.");
        }

        ObtenerFacturaUseCase obtenerFactura =
            new ObtenerFacturaUseCase(_unitOfWork);

        return await obtenerFactura.ExecuteAsync(nuevaFactura.Id);
    }
}
