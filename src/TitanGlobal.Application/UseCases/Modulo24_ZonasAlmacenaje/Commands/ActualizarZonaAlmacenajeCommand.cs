using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje.Commands;

public class ActualizarZonaAlmacenajeCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarZonaRequestDto Solicitud { get; set; }
}

public class ActualizarZonaAlmacenajeCommandHandler : IRequestHandler<ActualizarZonaAlmacenajeCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarZonaAlmacenajeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarZonaAlmacenajeCommand request, CancellationToken cancellationToken)
    {
        ZonaAlmacenaje? zona = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (zona is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La zona {request.Id} no existe.");
        }

        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(almacen => almacen.Id == request.Solicitud.AlmacenId);
        Almacen almacen = ValidacionZonaAlmacenaje.ValidarAlmacenExistente(almacenEncontrado, request.Solicitud.AlmacenId);
        if (almacen.Id != zona.AlmacenId)
        {
            ValidacionZonaAlmacenaje.ValidarAlmacenActivo(almacen);
        }

        string codigo = ValidacionZonaAlmacenaje.NormalizarCodigo(request.Solicitud.Codigo);
        string nombre = ValidacionZonaAlmacenaje.NormalizarNombre(request.Solicitud.Nombre);
        string tipo = ValidacionZonaAlmacenaje.NormalizarTipo(request.Solicitud.Tipo);
        string temperatura = ValidacionZonaAlmacenaje.NormalizarTemperatura(request.Solicitud.Temperatura);
        decimal capacidadM3 = ValidacionZonaAlmacenaje.ValidarCapacidad(request.Solicitud.CapacidadM3);
        bool codigoExiste = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != request.Id && entidad.AlmacenId == almacen.Id && entidad.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionZonaAlmacenaje.CrearConflictoCodigo(codigo, almacen.Codigo);
        }

        if (request.Solicitud.Activa)
        {
            decimal capacidadOtrasZonasM3 = await _unitOfWork.Repository<ZonaAlmacenaje>()
                .AsQueryable()
                .Where(entidad => entidad.Id != request.Id && entidad.AlmacenId == almacen.Id && entidad.Activa)
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
        zona.Activa = request.Solicitud.Activa;
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
