using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion.Commands;

public class CrearRutaProduccionCommand : IRequest<int>
{
    public required CrearRutaProduccionRequestDto Solicitud { get; set; }
}

public class CrearRutaProduccionCommandHandler : IRequestHandler<CrearRutaProduccionCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public CrearRutaProduccionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(CrearRutaProduccionCommand request, CancellationToken cancellationToken)
    {
        ValidacionRutaProduccion.ValidarIdProducto(request.Solicitud.ProductoId);
        bool existeProducto = await _unidad.Repository<Producto>().AsQueryable()
            .AnyAsync(producto => producto.Id == request.Solicitud.ProductoId);
        if (!existeProducto)
        {
            throw ValidacionRutaProduccion.ProductoInexistente(request.Solicitud.ProductoId);
        }

        if (request.Solicitud.Version <= 0)
        {
            throw ValidacionRutaProduccion.Invalida("La versión debe ser mayor que 0.");
        }

        List<OperacionRutaProduccion> operaciones =
            await ValidacionRutaProduccion.ValidarOperacionesAsync(_unidad, request.Solicitud.Operaciones);
        IGenericRepository<RutaProduccion> repositorio = _unidad.Repository<RutaProduccion>();
        bool existeVersion = await repositorio.AsQueryable()
            .AnyAsync(ruta => ruta.ProductoId == request.Solicitud.ProductoId && ruta.Version == request.Solicitud.Version);
        if (existeVersion)
        {
            throw VersionDuplicada(request.Solicitud);
        }

        RutaProduccion ruta = new RutaProduccion
        {
            ProductoId = request.Solicitud.ProductoId,
            Version = request.Solicitud.Version,
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
            throw VersionDuplicada(request.Solicitud);
        }

        return ruta.Id;
    }

    private static ReglaNegocioException VersionDuplicada(CrearRutaProduccionRequestDto solicitud)
    {
        return ValidacionRutaProduccion.Conflicto(
            $"Ya existe la versión {solicitud.Version} para el producto {solicitud.ProductoId}.");
    }
}
