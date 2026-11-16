using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class ActualizarLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, ActualizarLicitacionRequestDto solicitud)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(id);
        }

        if (licitacion.Estado != ValidacionLicitacion.Abierta)
        {
            throw ValidacionLicitacion.NoAbierta(id);
        }

        string titulo = ValidacionLicitacion.NormalizarTitulo(solicitud.Titulo);
        ValidacionLicitacion.ValidarPresupuesto(solicitud.PresupuestoReferencial);
        DateTime fechaCierre = ValidacionLicitacion.ValidarFechaCierre(solicitud.FechaCierre);
        string? descripcion = ValidacionLicitacion.NormalizarDescripcion(solicitud.Descripcion);

        bool hayOfertaMayor = await _unitOfWork.Repository<OfertaLicitacion>()
            .AsQueryable()
            .AnyAsync(oferta => oferta.LicitacionId == id && oferta.Monto > solicitud.PresupuestoReferencial);
        if (hayOfertaMayor)
        {
            throw ValidacionLicitacion.Error("El presupuesto no puede ser menor a una oferta ya presentada.");
        }

        licitacion.Titulo = titulo;
        licitacion.Descripcion = descripcion;
        licitacion.PresupuestoReferencial = solicitud.PresupuestoReferencial;
        licitacion.FechaCierre = fechaCierre;
        await _unitOfWork.CompleteAsync();

        return licitacion.Id;
    }
}
