using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Commands;

public class ActualizarVehiculoFlotaCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarVehiculoFlotaRequestDto Solicitud { get; set; }
}

public class ActualizarVehiculoFlotaCommandHandler : IRequestHandler<ActualizarVehiculoFlotaCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarVehiculoFlotaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarVehiculoFlotaCommand request, CancellationToken cancellationToken)
    {
        string placa = ValidacionVehiculoFlota.NormalizarPlaca(request.Solicitud.Placa);
        string marca = ValidacionVehiculoFlota.NormalizarMarca(request.Solicitud.Marca);
        string modelo = ValidacionVehiculoFlota.NormalizarModelo(request.Solicitud.Modelo);
        int anio = ValidacionVehiculoFlota.ValidarAnio(request.Solicitud.Anio);
        string tipo = ValidacionVehiculoFlota.NormalizarTipo(request.Solicitud.Tipo);
        decimal capacidadKg = ValidacionVehiculoFlota.ValidarCapacidadKg(request.Solicitud.CapacidadKg);
        string? observaciones = ValidacionVehiculoFlota.NormalizarObservaciones(request.Solicitud.Observaciones);

        VehiculoFlota? vehiculo = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .FirstOrDefaultAsync(v => v.Id == request.Id);

        if (vehiculo is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El vehículo {request.Id} no existe.");
        }

        string placaNormalizada = placa.ToLowerInvariant();
        bool placaEnUso = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .AnyAsync(v => v.Placa.Trim().ToLower() == placaNormalizada && v.Id != request.Id);

        if (placaEnUso)
        {
            throw CrearConflictoPlacaException(placa);
        }

        vehiculo.Placa = placa;
        vehiculo.Marca = marca;
        vehiculo.Modelo = modelo;
        vehiculo.Anio = anio;
        vehiculo.Tipo = tipo;
        vehiculo.CapacidadKg = capacidadKg;
        vehiculo.Activo = request.Solicitud.Activo;
        vehiculo.Observaciones = observaciones;

        _unitOfWork.Repository<VehiculoFlota>().Update(vehiculo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoPlacaException(placa);
        }

        return vehiculo.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    private static ReglaNegocioException CrearConflictoPlacaException(string placa)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un vehículo con la placa {placa}.");
    }
}
