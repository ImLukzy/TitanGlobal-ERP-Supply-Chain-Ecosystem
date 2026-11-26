using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public class ActualizarReglaReabastecimientoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarReglaReabastecimientoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReglaReabastecimientoListadoDto> ExecuteAsync(
        int id,
        ActualizarReglaReabastecimientoRequestDto solicitud)
    {
        ValidacionReabastecimiento.ValidarParametrosStock(
            solicitud.StockActual,
            solicitud.StockMinimo,
            solicitud.PuntoReorden,
            solicitud.StockMaximo,
            solicitud.CantidadLoteCompra);
        string? observaciones = ValidacionReabastecimiento.NormalizarObservaciones(solicitud.Observaciones);

        ReglaReabastecimiento? reglaEncontrada = await _unitOfWork.Repository<ReglaReabastecimiento>()
            .AsQueryable()
            .Include(registro => registro.Producto)
            .Include(registro => registro.Almacen)
            .Include(registro => registro.ProveedorPreferido)
            .SingleOrDefaultAsync(registro => registro.Id == id);
        ReglaReabastecimiento regla = ValidacionReabastecimiento.ValidarReglaExistente(reglaEncontrada, id);

        Proveedor? proveedorEncontrado = null;
        if (solicitud.ProveedorPreferidoId.HasValue)
        {
            proveedorEncontrado = await _unitOfWork.Repository<Proveedor>()
                .AsQueryable()
                .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProveedorPreferidoId.Value);
        }
        Proveedor? proveedor = ValidacionReabastecimiento.ValidarProveedorOpcional(
            proveedorEncontrado,
            solicitud.ProveedorPreferidoId);

        regla.ProveedorPreferidoId = proveedor?.Id;
        regla.ProveedorPreferido = proveedor;
        regla.StockActual = solicitud.StockActual;
        regla.StockMinimo = solicitud.StockMinimo;
        regla.PuntoReorden = solicitud.PuntoReorden;
        regla.StockMaximo = solicitud.StockMaximo;
        regla.CantidadLoteCompra = solicitud.CantidadLoteCompra;
        regla.Activa = solicitud.Activa;
        regla.Observaciones = observaciones;

        _unitOfWork.Repository<ReglaReabastecimiento>().Update(regla);
        await _unitOfWork.CompleteAsync();

        return ValidacionReabastecimiento.MapearListado(regla);
    }
}
