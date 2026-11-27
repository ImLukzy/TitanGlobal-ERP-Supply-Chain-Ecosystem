using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class MejorarOfertaLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public MejorarOfertaLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int licitacionId, int ofertaId, MejorarOfertaRequestDto solicitud)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == licitacionId);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(licitacionId);
        }

        OfertaLicitacion? oferta = await _unitOfWork.Repository<OfertaLicitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == ofertaId && entidad.LicitacionId == licitacionId);
        if (oferta is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La oferta {ofertaId} no existe en la licitación {licitacionId}.");
        }

        if (licitacion.Tipo != ValidacionLicitacion.SubastaInversa)
        {
            throw ValidacionLicitacion.Conflicto("Solo las subastas inversas admiten mejorar la oferta.");
        }

        if (licitacion.Estado != ValidacionLicitacion.Abierta)
        {
            throw ValidacionLicitacion.NoAbierta(licitacionId);
        }

        if (licitacion.FechaCierre <= DateTime.UtcNow)
        {
            throw ValidacionLicitacion.PlazoVencido(licitacionId);
        }

        ValidacionLicitacion.ValidarMonto(solicitud.Monto);
        if (solicitud.Monto >= oferta.Monto)
        {
            throw ValidacionLicitacion.Error("La nueva oferta debe ser menor a la actual.");
        }

        oferta.Monto = solicitud.Monto;
        await _unitOfWork.CompleteAsync();

        return oferta.Id;
    }
}
