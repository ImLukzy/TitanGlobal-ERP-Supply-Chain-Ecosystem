using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class CrearLoteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearLoteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarLoteRequestDto solicitud)
    {
        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProductoId);
        Producto producto = ValidacionLoteSerie.ValidarProductoExistente(productoEncontrado, solicitud.ProductoId);
        string codigo = ValidacionLoteSerie.NormalizarCodigoLote(solicitud.Codigo);
        int cantidad = ValidacionLoteSerie.ValidarCantidadLote(solicitud.Cantidad);
        string estado = ValidacionLoteSerie.NormalizarEstadoLote(solicitud.Estado);
        DateTime? fabricacion = ValidacionLoteSerie.NormalizarFecha(solicitud.FechaFabricacion);
        DateTime? vencimiento = ValidacionLoteSerie.NormalizarFecha(solicitud.FechaVencimiento);
        ValidacionLoteSerie.ValidarFechas(fabricacion, vencimiento);
        bool codigoExiste = await _unitOfWork.Repository<Lote>()
            .AsQueryable()
            .AnyAsync(registro => registro.ProductoId == producto.Id && registro.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionLoteSerie.CrearConflictoCodigoLote(codigo, producto.Id);
        }

        Lote loteNuevo = new Lote
        {
            ProductoId = producto.Id,
            Codigo = codigo,
            FechaFabricacion = fabricacion,
            FechaVencimiento = vencimiento,
            Cantidad = cantidad,
            Estado = estado
        };
        await _unitOfWork.Repository<Lote>().AddAsync(loteNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw ValidacionLoteSerie.CrearConflictoCodigoLote(codigo, producto.Id);
        }

        return loteNuevo.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
