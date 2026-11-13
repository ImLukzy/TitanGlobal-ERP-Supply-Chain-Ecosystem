using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class CrearSerieUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearSerieUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarSerieRequestDto solicitud)
    {
        Producto? productoEncontrado = await _unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == solicitud.ProductoId);
        Producto producto = ValidacionLoteSerie.ValidarProductoExistente(productoEncontrado, solicitud.ProductoId);
        string numeroSerie = ValidacionLoteSerie.NormalizarNumeroSerie(solicitud.NumeroSerie);

        if (solicitud.LoteId.HasValue)
        {
            int loteId = solicitud.LoteId.Value;
            Lote? lote = await _unitOfWork.Repository<Lote>()
                .AsQueryable()
                .SingleOrDefaultAsync(registro => registro.Id == loteId);
            if (lote is null)
            {
                throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, $"El lote {loteId} no existe.");
            }

            if (lote.ProductoId != producto.Id)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict,
                    $"El lote {loteId} no pertenece al producto {producto.Id}.");
            }

            if (lote.Estado == "Bloqueado")
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict,
                    $"El lote {lote.Codigo} está bloqueado.");
            }

            int seriesRegistradas = await _unitOfWork.Repository<UnidadSerie>()
                .AsQueryable()
                .CountAsync(registro => registro.LoteId == loteId);
            if (seriesRegistradas >= lote.Cantidad)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status409Conflict,
                    $"El lote {lote.Codigo} ya tiene registradas todas sus series ({lote.Cantidad}).");
            }
        }

        bool numeroExiste = await _unitOfWork.Repository<UnidadSerie>()
            .AsQueryable()
            .AnyAsync(registro => registro.ProductoId == producto.Id && registro.NumeroSerie == numeroSerie);
        if (numeroExiste)
        {
            throw ValidacionLoteSerie.CrearConflictoNumeroSerie(numeroSerie, producto.Id);
        }

        UnidadSerie serieNueva = new UnidadSerie
        {
            ProductoId = producto.Id,
            LoteId = solicitud.LoteId,
            NumeroSerie = numeroSerie,
            Estado = "Disponible"
        };
        await _unitOfWork.Repository<UnidadSerie>().AddAsync(serieNueva);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw ValidacionLoteSerie.CrearConflictoNumeroSerie(numeroSerie, producto.Id);
        }

        return serieNueva.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
