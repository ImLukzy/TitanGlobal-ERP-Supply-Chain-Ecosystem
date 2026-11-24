using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

internal static class PersistenciaNotaCreditoDebito
{
    private const string IndiceNumeracion = "IX_NotasCreditoDebito_Numero";

    public static async Task<NotaCreditoDebitoDetalleDto> ObtenerDetalleAsync(IUnitOfWork unitOfWork, int id)
    {
        NotaCreditoDebitoDetalleDto? detalle = await unitOfWork.Repository<NotaCreditoDebito>()
            .AsQueryable()
            .AsNoTracking()
            .Where(nota => nota.Id == id)
            .Select(nota => new NotaCreditoDebitoDetalleDto
            {
                Id = nota.Id,
                FacturaId = nota.FacturaId,
                Tipo = nota.Tipo,
                Numero = nota.Numero,
                Estado = nota.Estado,
                BaseImponible = nota.BaseImponible,
                ImporteImpuesto = nota.ImporteImpuesto,
                Total = nota.Total,
                Moneda = nota.Moneda,
                FechaCreacion = nota.FechaCreacion,
                FechaEmision = nota.FechaEmision,
                ClienteId = nota.Factura.ClienteId,
                ClienteNombre = nota.Factura.Cliente.Nombre,
                FacturaTipoComprobante = nota.Factura.TipoComprobante,
                FacturaSerie = nota.Factura.Serie,
                FacturaNumero = nota.Factura.Numero,
                Motivo = nota.Motivo,
                FechaAnulacion = nota.FechaAnulacion,
                MotivoAnulacion = nota.MotivoAnulacion,
                Detalles = nota.DetallesNotaCreditoDebito.OrderBy(linea => linea.Id)
                    .Select(linea => new LineaNotaCreditoDebitoDto
                    {
                        Id = linea.Id,
                        DetalleFacturaId = linea.DetalleFacturaId,
                        Cantidad = linea.Cantidad,
                        PrecioUnitario = linea.PrecioUnitario,
                        PorcentajeImpuesto = linea.PorcentajeImpuesto,
                        BaseImponible = linea.BaseImponible,
                        ImporteImpuesto = linea.ImporteImpuesto,
                        Total = linea.Total
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();

        return detalle ?? throw NotaInexistente();
    }

    public static async Task<Factura> ObtenerFacturaAsync(IUnitOfWork unitOfWork, int facturaId)
    {
        Factura? factura = await unitOfWork.Repository<Factura>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(origen => origen.Id == facturaId);
        return factura ?? throw new ReglaNegocioException(
            CodigosEstado.Status404NotFound, "La factura de origen no existe.");
    }

    public static void ValidarFacturaEmitida(Factura factura)
    {
        if (factura.Estado != ValidacionFactura.EstadoEmitida)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict, "El estado de la factura no permite registrar la nota.");
        }
    }

    public static async Task ActualizarDetallesAsync(
        IUnitOfWork unitOfWork, NotaCreditoDebito nota, List<DetalleNotaCreditoDebito> detalles)
    {
        Dictionary<int, DetalleNotaCreditoDebito> nuevos = detalles.ToDictionary(linea => linea.DetalleFacturaId);
        IGenericRepository<DetalleNotaCreditoDebito> repositorio = unitOfWork.Repository<DetalleNotaCreditoDebito>();
        foreach (DetalleNotaCreditoDebito actual in nota.DetallesNotaCreditoDebito.ToList())
        {
            if (nuevos.Remove(actual.DetalleFacturaId, out DetalleNotaCreditoDebito? nuevo))
            {
                actual.Cantidad = nuevo.Cantidad;
                actual.PrecioUnitario = nuevo.PrecioUnitario;
                actual.PorcentajeImpuesto = nuevo.PorcentajeImpuesto;
                actual.BaseImponible = nuevo.BaseImponible;
                actual.ImporteImpuesto = nuevo.ImporteImpuesto;
                actual.Total = nuevo.Total;
            }
            else
            {
                repositorio.Remove(actual);
            }
        }

        foreach (DetalleNotaCreditoDebito nueva in nuevos.Values)
        {
            nueva.NotaCreditoDebitoId = nota.Id;
            await repositorio.AddAsync(nueva);
        }
    }

    public static async Task ValidarTopesCreditoAsync(IUnitOfWork unitOfWork, NotaCreditoDebito nota)
    {
        int[] ids = nota.DetallesNotaCreditoDebito.Select(linea => linea.DetalleFacturaId).ToArray();
        Dictionary<int, ConsumoCredito> acumulados = await unitOfWork.Repository<DetalleNotaCreditoDebito>()
            .AsQueryable()
            .AsNoTracking()
            .Where(linea => ids.Contains(linea.DetalleFacturaId)
                && linea.NotaCreditoDebito.FacturaId == nota.FacturaId
                && linea.NotaCreditoDebito.Tipo == NotaCreditoDebito.TipoCredito
                && linea.NotaCreditoDebito.Estado == NotaCreditoDebito.EstadoEmitida)
            .GroupBy(linea => linea.DetalleFacturaId)
            .Select(grupo => new ConsumoCredito(grupo.Key, grupo.Sum(linea => linea.Cantidad), grupo.Sum(linea => linea.Total)))
            .ToDictionaryAsync(acumulado => acumulado.DetalleFacturaId);

        foreach (DetalleNotaCreditoDebito linea in nota.DetallesNotaCreditoDebito.OrderBy(linea => linea.Id))
        {
            acumulados.TryGetValue(linea.DetalleFacturaId, out ConsumoCredito? acumulado);
            if ((acumulado?.Cantidad ?? 0) + linea.Cantidad > linea.DetalleFactura.Cantidad)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict, "El crédito supera la cantidad disponible de la línea de factura.");
            }

            if ((acumulado?.Total ?? 0) + linea.Total > linea.DetalleFactura.Total)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict, "El crédito supera el importe disponible de la línea de factura.");
            }
        }
    }

    public static async Task GuardarAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            await unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (
            excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceNumeracion)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "Ya existe una nota con ese número.");
        }
        catch (ViolacionRestriccionException excepcion) when (
            excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea && excepcion.Restriccion is
                "FK_NotasCreditoDebito_Facturas_FacturaId" or
                "FK_DetallesNotaCreditoDebito_NotasCreditoDebito_NotaId" or
                "FK_DetallesNotaCreditoDebito_DetallesFactura_DetalleFacturaId")
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict, "No se pudo guardar la nota porque una referencia ya no existe.");
        }
    }

    public static async Task<NotaCreditoDebitoDetalleDto> ModificarAsync(
        IUnitOfWorkFacturas unitOfWork, int id, Func<NotaCreditoDebito, Task> modificar)
    {
        IGenericRepository<NotaCreditoDebito> repositorio = unitOfWork.Repository<NotaCreditoDebito>();
        int? facturaId = await repositorio.AsQueryable()
            .AsNoTracking()
            .Where(nota => nota.Id == id)
            .Select(nota => (int?)nota.FacturaId)
            .SingleOrDefaultAsync();

        if (!facturaId.HasValue)
        {
            throw NotaInexistente();
        }

        return await unitOfWork.ConFacturaBloqueadaAsync(facturaId.Value, ModificarBajoBloqueoAsync);

        async Task<NotaCreditoDebitoDetalleDto> ModificarBajoBloqueoAsync()
        {
            // La primera lectura con seguimiento ocurre después de adquirir el bloqueo.
            NotaCreditoDebito? nota = await repositorio.AsQueryable()
                .AsTracking()
                .Include(entidad => entidad.DetallesNotaCreditoDebito)
                .ThenInclude(linea => linea.DetalleFactura)
                .SingleOrDefaultAsync(entidad => entidad.Id == id);
            if (nota is null)
            {
                throw NotaInexistente();
            }

            await modificar(nota);
            await GuardarAsync(unitOfWork);
            return await ObtenerDetalleAsync(unitOfWork, id);
        }
    }

    private static ReglaNegocioException NotaInexistente()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound, "La nota de crédito o débito no existe.");
    }

    private sealed record ConsumoCredito(int DetalleFacturaId, decimal Cantidad, decimal Total);
}
