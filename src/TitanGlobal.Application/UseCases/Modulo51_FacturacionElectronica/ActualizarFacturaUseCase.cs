using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;

public class ActualizarFacturaUseCase
{
    private readonly IUnitOfWorkFacturas _unitOfWork;

    public ActualizarFacturaUseCase(IUnitOfWorkFacturas unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<FacturaDetalleDto> ExecuteAsync(
        int id,
        GuardarFacturaRequestDto request)
    {
        return _unitOfWork.ConFacturaBloqueadaAsync(id, ActualizarBajoBloqueoAsync);

        Task<FacturaDetalleDto> ActualizarBajoBloqueoAsync()
        {
            return ActualizarAsync(id, request);
        }
    }

    private async Task<FacturaDetalleDto> ActualizarAsync(int id, GuardarFacturaRequestDto request)
    {
        IGenericRepository<Factura> repositorioFacturas =
            _unitOfWork.Repository<Factura>();

        IGenericRepository<DetalleFactura> repositorioDetalles =
            _unitOfWork.Repository<DetalleFactura>();

        IGenericRepository<Cliente> repositorioClientes =
            _unitOfWork.Repository<Cliente>();

        IGenericRepository<OrdenCompra> repositorioOrdenes =
            _unitOfWork.Repository<OrdenCompra>();

        Factura? factura = await repositorioFacturas
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
                "Solo se pueden modificar facturas en estado Borrador.");
        }

        ValidacionFactura.ValidarSolicitud(request);

        bool clienteExiste = await repositorioClientes
            .AsQueryable()
            .AnyAsync(cliente => cliente.Id == request.ClienteId);

        if (!clienteExiste)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "El cliente seleccionado no existe.");
        }

        if (request.OrdenCompraId.HasValue)
        {
            OrdenCompra? ordenCompra = await repositorioOrdenes
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
            .AnyAsync(otraFactura =>
                otraFactura.Id != id &&
                otraFactura.TipoComprobante == request.TipoComprobante &&
                otraFactura.Serie == request.Serie &&
                otraFactura.Numero == request.Numero);

        if (comprobanteExistente)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "Ya existe un comprobante con la misma serie y número.");
        }

        CalculoFactura calculoFactura =
            new CalculoFactura(_unitOfWork);

        List<DetalleFactura> nuevosDetalles =
            await calculoFactura.CalcularDetallesAsync(request);

        Factura facturaCalculada = new Factura
        {
            DetallesFactura = nuevosDetalles
        };

        CalculoFactura.CalcularTotales(facturaCalculada);

        List<DetalleFactura> detallesAnteriores =
            factura.DetallesFactura.ToList();

        foreach (DetalleFactura detalle in detallesAnteriores)
        {
            repositorioDetalles.Remove(detalle);
        }

        factura.DetallesFactura.Clear();

        factura.ClienteId = request.ClienteId;

        factura.OrdenCompraId = request.OrdenCompraId;

        factura.TipoComprobante = request.TipoComprobante;

        factura.Serie = request.Serie;

        factura.Numero = request.Numero;

        factura.FechaEmision = request.FechaEmision!.Value;

        factura.FechaVencimiento = request.FechaVencimiento;

        factura.Moneda = request.Moneda;

        factura.Observaciones = request.Observaciones;

        factura.Subtotal = facturaCalculada.Subtotal;

        factura.TotalDescuento = facturaCalculada.TotalDescuento;

        factura.TotalImpuesto = facturaCalculada.TotalImpuesto;

        factura.Total = facturaCalculada.Total;

        foreach (DetalleFactura detalle in nuevosDetalles)
        {
            detalle.FacturaId = factura.Id;

            detalle.Factura = factura;

            factura.DetallesFactura.Add(detalle);

            await repositorioDetalles.AddAsync(detalle);
        }

        try
        {
            repositorioFacturas.Update(factura);

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
                "No se pudo actualizar la factura porque una referencia ya no existe.");
        }

        ObtenerFacturaUseCase obtenerFactura =
            new ObtenerFacturaUseCase(_unitOfWork);

        return await obtenerFactura.ExecuteAsync(factura.Id);
    }
}
