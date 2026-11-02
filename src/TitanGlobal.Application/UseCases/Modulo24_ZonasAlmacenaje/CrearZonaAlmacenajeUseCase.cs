using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

public class CrearZonaAlmacenajeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearZonaAlmacenajeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarZonaRequestDto solicitud)
    {
        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(almacen => almacen.Id == solicitud.AlmacenId);
        Almacen almacen = ValidacionZonaAlmacenaje.ValidarAlmacenExistente(almacenEncontrado, solicitud.AlmacenId);
        ValidacionZonaAlmacenaje.ValidarAlmacenActivo(almacen);
        string codigo = ValidacionZonaAlmacenaje.NormalizarCodigo(solicitud.Codigo);
        string nombre = ValidacionZonaAlmacenaje.NormalizarNombre(solicitud.Nombre);
        string tipo = ValidacionZonaAlmacenaje.NormalizarTipo(solicitud.Tipo);
        string temperatura = ValidacionZonaAlmacenaje.NormalizarTemperatura(solicitud.Temperatura);
        decimal capacidadM3 = ValidacionZonaAlmacenaje.ValidarCapacidad(solicitud.CapacidadM3);
        bool codigoExiste = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .AnyAsync(zona => zona.AlmacenId == almacen.Id && zona.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionZonaAlmacenaje.CrearConflictoCodigo(codigo, almacen.Codigo);
        }

        if (solicitud.Activa)
        {
            decimal capacidadOtrasZonasM3 = await _unitOfWork.Repository<ZonaAlmacenaje>()
                .AsQueryable()
                .Where(zona => zona.AlmacenId == almacen.Id && zona.Activa)
                .Select(zona => (decimal?)zona.CapacidadM3)
                .SumAsync() ?? 0m;
            ValidacionZonaAlmacenaje.ValidarCapacidadAsignada(almacen, capacidadOtrasZonasM3, capacidadM3);
        }

        ZonaAlmacenaje zonaNueva = new ZonaAlmacenaje
        {
            AlmacenId = almacen.Id,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Temperatura = temperatura,
            CapacidadM3 = capacidadM3,
            Activa = solicitud.Activa
        };
        await _unitOfWork.Repository<ZonaAlmacenaje>().AddAsync(zonaNueva);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaZonaAlmacenaje.EsViolacionIndiceUnico(excepcion))
        {
            throw ValidacionZonaAlmacenaje.CrearConflictoCodigo(codigo, almacen.Codigo);
        }

        return zonaNueva.Id;
    }
}
