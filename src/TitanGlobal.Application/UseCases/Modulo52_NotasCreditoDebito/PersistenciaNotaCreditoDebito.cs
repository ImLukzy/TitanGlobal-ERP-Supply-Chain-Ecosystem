using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Application.UseCases.Modulo51_FacturacionElectronica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo52_NotasCreditoDebito;

internal static class PersistenciaNotaCreditoDebito
{
    private const string IndiceNumeracion = "IX_NotasCreditoDebito_Tipo_Serie_Numero";
    private const string ClaveFactura = "FK_NotasCreditoDebito_Facturas_FacturaId";

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
                Serie = nota.Serie,
                Numero = nota.Numero,
                Estado = nota.Estado,
                Total = nota.Total,
                Moneda = nota.Factura.Moneda,
                FechaCreacion = nota.FechaCreacion,
                FechaEmision = nota.FechaEmision,
                ClienteId = nota.Factura.ClienteId,
                ClienteNombre = nota.Factura.Cliente.Nombre,
                FacturaTipoComprobante = nota.Factura.TipoComprobante,
                FacturaSerie = nota.Factura.Serie,
                FacturaNumero = nota.Factura.Numero,
                Motivo = nota.Motivo,
                FechaAnulacion = nota.FechaAnulacion,
                MotivoAnulacion = nota.MotivoAnulacion
            })
            .SingleOrDefaultAsync();

        return detalle ?? throw NotaInexistente();
    }

    public static async Task ValidarFacturaEmitidaAsync(IUnitOfWork unitOfWork, int facturaId)
    {
        string? estado = await unitOfWork.Repository<Factura>()
            .AsQueryable()
            .AsNoTracking()
            .Where(factura => factura.Id == facturaId)
            .Select(factura => factura.Estado)
            .SingleOrDefaultAsync();

        if (estado is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "La factura de origen no existe.");
        }

        if (estado != ValidacionFactura.EstadoEmitida)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "El estado de la factura no permite registrar la nota.");
        }
    }

    public static async Task ValidarNumeracionAsync(
        IUnitOfWork unitOfWork, DatosNotaCreditoDebito datos, int? notaActualId = null)
    {
        bool duplicada = await unitOfWork.Repository<NotaCreditoDebito>()
            .AsQueryable()
            .AnyAsync(nota => nota.Tipo == datos.Tipo
                && nota.Serie == datos.Serie
                && nota.Numero == datos.Numero
                && nota.Id != notaActualId);

        if (duplicada)
        {
            throw NumeracionDuplicada();
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
            throw NumeracionDuplicada();
        }
        catch (ViolacionRestriccionException excepcion) when (
            excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea && excepcion.Restriccion == ClaveFactura)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                "No se pudo guardar la nota porque una referencia ya no existe.");
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
            NotaCreditoDebito? nota = await repositorio.AsQueryable()
                .SingleOrDefaultAsync(entidad => entidad.Id == id);
            if (nota is null)
            {
                throw NotaInexistente();
            }

            await modificar(nota);
            repositorio.Update(nota);
            await GuardarAsync(unitOfWork);
            return await ObtenerDetalleAsync(unitOfWork, id);
        }
    }

    private static ReglaNegocioException NotaInexistente()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound,
            "La nota de crédito o débito no existe.");
    }

    private static ReglaNegocioException NumeracionDuplicada()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            "Ya existe una nota con la misma serie y número.");
    }
}
