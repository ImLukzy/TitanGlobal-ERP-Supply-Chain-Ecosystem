using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo14_Licitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo14_Licitaciones;

public class CrearOfertaLicitacionUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearOfertaLicitacionUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int licitacionId, CrearOfertaRequestDto solicitud)
    {
        Licitacion? licitacion = await _unitOfWork.Repository<Licitacion>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == licitacionId);
        if (licitacion is null)
        {
            throw ValidacionLicitacion.NoExiste(licitacionId);
        }

        if (licitacion.Estado != ValidacionLicitacion.Abierta)
        {
            throw ValidacionLicitacion.NoAbierta(licitacionId);
        }

        if (licitacion.FechaCierre <= DateTime.UtcNow)
        {
            throw ValidacionLicitacion.PlazoVencido(licitacionId);
        }

        if (solicitud.ProveedorId <= 0)
        {
            throw ValidacionLicitacion.Error("El proveedor es obligatorio.");
        }

        ValidacionLicitacion.ValidarMonto(solicitud.Monto);
        if (solicitud.Monto > licitacion.PresupuestoReferencial)
        {
            throw ValidacionLicitacion.Error("El monto no puede superar el presupuesto referencial.");
        }

        ValidacionLicitacion.ValidarPlazo(solicitud.PlazoEntregaDias);
        string? observaciones = ValidacionLicitacion.NormalizarObservaciones(solicitud.Observaciones);

        bool? proveedorActivo = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Where(proveedor => proveedor.Id == solicitud.ProveedorId)
            .Select(proveedor => (bool?)proveedor.Activo)
            .SingleOrDefaultAsync();
        if (proveedorActivo is null)
        {
            throw ValidacionLicitacion.Error($"El proveedor {solicitud.ProveedorId} no existe.");
        }

        if (!proveedorActivo.Value)
        {
            throw ValidacionLicitacion.Error($"El proveedor {solicitud.ProveedorId} está inactivo.");
        }

        bool yaOferto = await _unitOfWork.Repository<OfertaLicitacion>()
            .AsQueryable()
            .AnyAsync(oferta => oferta.LicitacionId == licitacionId && oferta.ProveedorId == solicitud.ProveedorId);
        if (yaOferto)
        {
            throw CrearConflictoOferta(solicitud.ProveedorId, licitacionId);
        }

        OfertaLicitacion ofertaNueva = new OfertaLicitacion
        {
            LicitacionId = licitacionId,
            ProveedorId = solicitud.ProveedorId,
            Monto = solicitud.Monto,
            PlazoEntregaDias = solicitud.PlazoEntregaDias,
            Observaciones = observaciones,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<OfertaLicitacion>().AddAsync(ofertaNueva);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoOferta(solicitud.ProveedorId, licitacionId);
        }

        return ofertaNueva.Id;
    }

    private static ReglaNegocioException CrearConflictoOferta(int proveedorId, int licitacionId)
    {
        return ValidacionLicitacion.Conflicto(
            $"El proveedor {proveedorId} ya presentó una oferta en la licitación {licitacionId}.");
    }
}
