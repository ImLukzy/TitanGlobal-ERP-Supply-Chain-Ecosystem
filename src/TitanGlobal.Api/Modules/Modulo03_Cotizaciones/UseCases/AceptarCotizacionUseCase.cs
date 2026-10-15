using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.UseCases;

public class AceptarCotizacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AceptarCotizacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CotizacionDetalleDto> ExecuteAsync(int id)
    {
        Cotizacion? cotizacion = await _unitOfWork.Repository<Cotizacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (cotizacion is null)
        {
            throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
        }

        ActualizarCotizacionUseCase.ExigirEmitida(cotizacion);
        if (ConsultaCotizacion.EsVencida(cotizacion.Estado, cotizacion.FechaValidez))
        {
            throw new ReglaNegocioException(
                StatusCodes.Status409Conflict,
                $"La cotización {cotizacion.Numero} está vencida.");
        }

        string numeroOrden = $"OC-{cotizacion.Numero}";
        bool numeroOrdenExiste = await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .AnyAsync(orden => orden.NumeroOrden == numeroOrden);
        if (numeroOrdenExiste)
        {
            throw CrearConflictoNumeroOrden(numeroOrden);
        }

        CotizacionDetalleDto cotizacionDetalle = await ConsultaCotizacion.ObtenerDetalleAsync(_unitOfWork, id)
            ?? throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
        DateTime ahoraUtc = FechasUtc.Normalizar(DateTime.UtcNow);
        OrdenCompra ordenCompra = new OrdenCompra
        {
            NumeroOrden = numeroOrden,
            Fecha = ahoraUtc,
            ClienteId = cotizacion.ClienteId,
            Total = cotizacionDetalle.Total,
            Descripcion = $"Generada desde la cotización {cotizacion.Numero}"
        };
        foreach (LineaCotizacionDto linea in cotizacionDetalle.Lineas)
        {
            decimal precioUnitario = decimal.Round(
                linea.Importe / linea.Cantidad,
                2,
                MidpointRounding.AwayFromZero);
            ordenCompra.Detalles.Add(new DetalleOrdenCompra
            {
                ProductoId = linea.ProductoId,
                Cantidad = linea.Cantidad,
                PrecioUnitario = precioUnitario
            });
        }

        cotizacion.Estado = "Aceptada";
        cotizacion.FechaDecision = ahoraUtc;
        try
        {
            using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _unitOfWork.Repository<OrdenCompra>().AddAsync(ordenCompra);
                await _unitOfWork.CompleteAsync();
                cotizacion.OrdenCompraId = ordenCompra.Id;
                _unitOfWork.Repository<Cotizacion>().Update(cotizacion);
                await _unitOfWork.CompleteAsync();
                transaccion.Complete();
            }
        }
        catch (DbUpdateException excepcion) when (EsViolacionNumeroUnico(excepcion))
        {
            throw CrearConflictoNumeroOrden(numeroOrden);
        }

        return await ConsultaCotizacion.ObtenerDetalleAsync(_unitOfWork, id)
            ?? throw ActualizarCotizacionUseCase.CrearNoEncontrada(id);
    }

    private static bool EsViolacionNumeroUnico(DbUpdateException excepcion)
    {
        PostgresException? postgres = excepcion.InnerException as PostgresException;
        return postgres?.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static ReglaNegocioException CrearConflictoNumeroOrden(string numeroOrden)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe una orden de compra con el número {numeroOrden}.");
    }
}
