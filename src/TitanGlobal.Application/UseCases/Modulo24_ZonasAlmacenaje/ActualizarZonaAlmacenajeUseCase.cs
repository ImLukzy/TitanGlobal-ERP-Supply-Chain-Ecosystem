using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

public class ActualizarZonaAlmacenajeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarZonaAlmacenajeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarZonaRequestDto solicitud)
    {
        ZonaAlmacenaje? zona = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (zona is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La zona {id} no existe.");
        }

        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(almacen => almacen.Id == solicitud.AlmacenId);
        Almacen almacen = ValidacionZonaAlmacenaje.ValidarAlmacenExistente(almacenEncontrado, solicitud.AlmacenId);
        if (almacen.Id != zona.AlmacenId)
        {
            ValidacionZonaAlmacenaje.ValidarAlmacenActivo(almacen);
        }

        string codigo = ValidacionZonaAlmacenaje.NormalizarCodigo(solicitud.Codigo);
        string nombre = ValidacionZonaAlmacenaje.NormalizarNombre(solicitud.Nombre);
        string tipo = ValidacionZonaAlmacenaje.NormalizarTipo(solicitud.Tipo);
        string temperatura = ValidacionZonaAlmacenaje.NormalizarTemperatura(solicitud.Temperatura);
        decimal capacidadM3 = ValidacionZonaAlmacenaje.ValidarCapacidad(solicitud.CapacidadM3);
        bool codigoExiste = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != id && entidad.AlmacenId == almacen.Id && entidad.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionZonaAlmacenaje.CrearConflictoCodigo(codigo, almacen.Codigo);
        }

        if (solicitud.Activa)
        {
            decimal capacidadOtrasZonasM3 = await _unitOfWork.Repository<ZonaAlmacenaje>()
                .AsQueryable()
                .Where(entidad => entidad.Id != id && entidad.AlmacenId == almacen.Id && entidad.Activa)
                .Select(entidad => (decimal?)entidad.CapacidadM3)
                .SumAsync() ?? 0m;
            ValidacionZonaAlmacenaje.ValidarCapacidadAsignada(almacen, capacidadOtrasZonasM3, capacidadM3);
        }

        zona.AlmacenId = almacen.Id;
        zona.Codigo = codigo;
        zona.Nombre = nombre;
        zona.Tipo = tipo;
        zona.Temperatura = temperatura;
        zona.CapacidadM3 = capacidadM3;
        zona.Activa = solicitud.Activa;
        _unitOfWork.Repository<ZonaAlmacenaje>().Update(zona);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaZonaAlmacenaje.EsViolacionIndiceUnico(excepcion))
        {
            throw ValidacionZonaAlmacenaje.CrearConflictoCodigo(codigo, almacen.Codigo);
        }

        return zona.Id;
    }
}
