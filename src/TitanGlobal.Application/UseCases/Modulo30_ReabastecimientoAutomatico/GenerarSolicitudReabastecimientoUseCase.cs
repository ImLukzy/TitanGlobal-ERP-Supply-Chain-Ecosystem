using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class GenerarSolicitudReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GenerarSolicitudReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EjecucionReabastecimientoItemDto> ExecuteAsync(int id)
    {
        EjecucionReabastecimiento? ejecucionEncontrada = await _unitOfWork.Repository<EjecucionReabastecimiento>()
            .AsQueryable()
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.Producto)
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.Almacen)
            .Include(registro => registro.ReglaReabastecimiento)
                .ThenInclude(regla => regla.ProveedorPreferido)
            .Include(registro => registro.SolicitudCompra)
            .SingleOrDefaultAsync(registro => registro.Id == id);

        EjecucionReabastecimiento ejecucion = ValidacionReabastecimiento.ValidarEjecucionExistente(ejecucionEncontrada, id);
        ValidacionReabastecimiento.ValidarEjecucionPendiente(ejecucion);

        ReglaReabastecimiento regla = ejecucion.ReglaReabastecimiento;
        string numeroSolicitud = await GenerarNumeroSolicitudAsync();

        SolicitudCompra solicitudCompra = new SolicitudCompra
        {
            Numero = numeroSolicitud,
            Fecha = DateTime.UtcNow,
            Solicitante = "Reabastecimiento Automático",
            ProveedorId = regla.ProveedorPreferidoId,
            Estado = "Borrador",
            Observaciones = $"Generada automáticamente desde regla {regla.Codigo} para almacén {regla.Almacen.Codigo} (stock {ejecucion.StockActual}, punto reorden {regla.PuntoReorden})."
        };

        DetalleSolicitudCompra detalle = new DetalleSolicitudCompra
        {
            ProductoId = regla.ProductoId,
            Cantidad = ejecucion.CantidadSugerida,
            PrecioEstimado = regla.Producto.Precio
        };
        solicitudCompra.Detalles.Add(detalle);

        await _unitOfWork.Repository<SolicitudCompra>().AddAsync(solicitudCompra);

        ejecucion.Estado = "SolicitudGenerada";
        ejecucion.SolicitudCompra = solicitudCompra;
        ejecucion.Observacion = $"Solicitud de compra {numeroSolicitud} generada por {ejecucion.CantidadSugerida} unidades.";

        await _unitOfWork.CompleteAsync();

        return ValidacionReabastecimiento.MapearEjecucion(ejecucion);
    }

    private async Task<string> GenerarNumeroSolicitudAsync()
    {
        List<string> existentes = await _unitOfWork.Repository<SolicitudCompra>()
            .AsQueryable()
            .AsNoTracking()
            .Where(registro => registro.Numero.StartsWith("SC-REB-"))
            .Select(registro => registro.Numero)
            .ToListAsync();

        int maxCorrelativo = 0;
        foreach (string numero in existentes)
        {
            if (numero.Length > 7 &&
                int.TryParse(numero.Substring(7), NumberStyles.None, CultureInfo.InvariantCulture, out int correlativo) &&
                correlativo > maxCorrelativo)
            {
                maxCorrelativo = correlativo;
            }
        }

        return string.Format(CultureInfo.InvariantCulture, "SC-REB-{0:D5}", maxCorrelativo + 1);
    }
}
