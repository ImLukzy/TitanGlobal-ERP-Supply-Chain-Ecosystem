using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class AdjudicarLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdjudicarLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, AdjudicarLicitacionRequestDto solicitud)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(id);
        }

        if (licitacion.Estado != ValidacionLicitacion.Cerrada)
        {
            throw ValidacionLicitacion.Conflicto($"La licitación {id} debe estar cerrada para adjudicarla.");
        }

        OfertaLicitacion? oferta = await _unitOfWork.Repository<OfertaLicitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == solicitud.OfertaId && entidad.LicitacionId == id);
        if (oferta is null)
        {
            throw ValidacionLicitacion.Error(
                $"La oferta {solicitud.OfertaId} no pertenece a la licitación {id}.");
        }

        licitacion.Estado = ValidacionLicitacion.Adjudicada;
        licitacion.ProveedorAdjudicadoId = oferta.ProveedorId;
        licitacion.MontoAdjudicado = oferta.Monto;
        await _unitOfWork.CompleteAsync();

        return licitacion.Id;
    }
}
