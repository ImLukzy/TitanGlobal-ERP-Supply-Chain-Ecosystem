using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class ObtenerLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LicitacionDetalleDto> ExecuteAsync(int id)
    {
        LicitacionDetalleDto? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new LicitacionDetalleDto
            {
                Id = entidad.Id,
                Numero = entidad.Numero,
                Titulo = entidad.Titulo,
                Descripcion = entidad.Descripcion,
                Tipo = entidad.Tipo,
                Estado = entidad.Estado,
                PresupuestoReferencial = entidad.PresupuestoReferencial,
                FechaCierre = entidad.FechaCierre,
                ProveedorAdjudicadoId = entidad.ProveedorAdjudicadoId,
                ProveedorAdjudicadoNombre = entidad.ProveedorAdjudicado == null
                    ? null
                    : entidad.ProveedorAdjudicado.RazonSocial,
                MontoAdjudicado = entidad.MontoAdjudicado,
                FechaRegistro = entidad.FechaRegistro
            })
            .SingleOrDefaultAsync();
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(id);
        }

        int? adjudicado = licitacion.ProveedorAdjudicadoId;
        licitacion.Ofertas = await _unitOfWork.Repository<OfertaLicitacion>()
            .AsQueryable()
            .Where(oferta => oferta.LicitacionId == id)
            .OrderBy(oferta => oferta.Monto)
            .ThenBy(oferta => oferta.Id)
            .Select(oferta => new OfertaDto
            {
                Id = oferta.Id,
                ProveedorId = oferta.ProveedorId,
                ProveedorNombre = oferta.Proveedor.RazonSocial,
                Monto = oferta.Monto,
                PlazoEntregaDias = oferta.PlazoEntregaDias,
                Observaciones = oferta.Observaciones,
                FechaRegistro = oferta.FechaRegistro,
                EsGanadora = adjudicado != null && oferta.ProveedorId == adjudicado
            })
            .ToListAsync();

        return licitacion;
    }
}
