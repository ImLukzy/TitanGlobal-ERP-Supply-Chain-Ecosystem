using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion;

public class CrearRutaProduccionUseCase
{
    private readonly IUnitOfWork _unidad;

    public CrearRutaProduccionUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(CrearRutaProduccionRequestDto solicitud)
    {
        ValidacionRutaProduccion.ValidarIdProducto(solicitud.ProductoId);
        bool existeProducto = await _unidad.Repository<Producto>().AsQueryable()
            .AnyAsync(producto => producto.Id == solicitud.ProductoId);
        if (!existeProducto)
        {
            throw ValidacionRutaProduccion.ProductoInexistente(solicitud.ProductoId);
        }

        if (solicitud.Version <= 0)
        {
            throw ValidacionRutaProduccion.Invalida("La versión debe ser mayor que 0.");
        }

        List<OperacionRutaProduccion> operaciones =
            await ValidacionRutaProduccion.ValidarOperacionesAsync(_unidad, solicitud.Operaciones);
        IGenericRepository<RutaProduccion> repositorio = _unidad.Repository<RutaProduccion>();
        bool existeVersion = await repositorio.AsQueryable()
            .AnyAsync(ruta => ruta.ProductoId == solicitud.ProductoId && ruta.Version == solicitud.Version);
        if (existeVersion)
        {
            throw VersionDuplicada(solicitud);
        }

        RutaProduccion ruta = new RutaProduccion
        {
            ProductoId = solicitud.ProductoId,
            Version = solicitud.Version,
            Estado = ValidacionRutaProduccion.Borrador,
            OperacionesRutaProduccion = operaciones
        };
        await repositorio.AddAsync(ruta);

        try
        {
            await PersistenciaRutaProduccion.GuardarOperacionesAsync(_unidad, ruta.ProductoId, operaciones);
        }
        catch (ViolacionRestriccionException error) when (
            error.Tipo == TipoViolacionRestriccion.Unicidad && error.Restriccion == PersistenciaRutaProduccion.IndiceVersion)
        {
            throw VersionDuplicada(solicitud);
        }

        return ruta.Id;
    }

    private static ReglaNegocioException VersionDuplicada(CrearRutaProduccionRequestDto solicitud)
    {
        return ValidacionRutaProduccion.Conflicto(
            $"Ya existe la versión {solicitud.Version} para el producto {solicitud.ProductoId}.");
    }
}
