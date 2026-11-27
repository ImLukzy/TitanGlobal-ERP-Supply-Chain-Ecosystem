using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo24_ZonasAlmacenaje;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje.Commands;

public class CrearZonaAlmacenajeCommand : IRequest<int>
{
    public required GuardarZonaRequestDto Solicitud { get; set; }
}

public class CrearZonaAlmacenajeCommandHandler : IRequestHandler<CrearZonaAlmacenajeCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearZonaAlmacenajeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearZonaAlmacenajeCommand request, CancellationToken cancellationToken)
    {
        Almacen? almacenEncontrado = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(almacen => almacen.Id == request.Solicitud.AlmacenId);
        Almacen almacen = ValidacionZonaAlmacenaje.ValidarAlmacenExistente(almacenEncontrado, request.Solicitud.AlmacenId);
        ValidacionZonaAlmacenaje.ValidarAlmacenActivo(almacen);
        string codigo = ValidacionZonaAlmacenaje.NormalizarCodigo(request.Solicitud.Codigo);
        string nombre = ValidacionZonaAlmacenaje.NormalizarNombre(request.Solicitud.Nombre);
        string tipo = ValidacionZonaAlmacenaje.NormalizarTipo(request.Solicitud.Tipo);
        string temperatura = ValidacionZonaAlmacenaje.NormalizarTemperatura(request.Solicitud.Temperatura);
        decimal capacidadM3 = ValidacionZonaAlmacenaje.ValidarCapacidad(request.Solicitud.CapacidadM3);
        bool codigoExiste = await _unitOfWork.Repository<ZonaAlmacenaje>()
            .AsQueryable()
            .AnyAsync(zona => zona.AlmacenId == almacen.Id && zona.Codigo == codigo);
        if (codigoExiste)
        {
            throw ValidacionZonaAlmacenaje.CrearConflictoCodigo(codigo, almacen.Codigo);
        }

        if (request.Solicitud.Activa)
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
            Activa = request.Solicitud.Activa
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
