using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota.Commands;

public class CrearVehiculoFlotaCommand : IRequest<int>
{
    public required GuardarVehiculoFlotaRequestDto Solicitud { get; set; }
}

public class CrearVehiculoFlotaCommandHandler : IRequestHandler<CrearVehiculoFlotaCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearVehiculoFlotaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearVehiculoFlotaCommand request, CancellationToken cancellationToken)
    {
        string placa = ValidacionVehiculoFlota.NormalizarPlaca(request.Solicitud.Placa);
        string marca = ValidacionVehiculoFlota.NormalizarMarca(request.Solicitud.Marca);
        string modelo = ValidacionVehiculoFlota.NormalizarModelo(request.Solicitud.Modelo);
        int anio = ValidacionVehiculoFlota.ValidarAnio(request.Solicitud.Anio);
        string tipo = ValidacionVehiculoFlota.NormalizarTipo(request.Solicitud.Tipo);
        decimal capacidadKg = ValidacionVehiculoFlota.ValidarCapacidadKg(request.Solicitud.CapacidadKg);
        string? observaciones = ValidacionVehiculoFlota.NormalizarObservaciones(request.Solicitud.Observaciones);

        string placaNormalizada = placa.ToLowerInvariant();
        bool placaExiste = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .AnyAsync(vehiculo => vehiculo.Placa.Trim().ToLower() == placaNormalizada);

        if (placaExiste)
        {
            throw CrearConflictoPlacaException(placa);
        }

        VehiculoFlota vehiculoNuevo = new VehiculoFlota
        {
            Placa = placa,
            Marca = marca,
            Modelo = modelo,
            Anio = anio,
            Tipo = tipo,
            CapacidadKg = capacidadKg,
            Activo = request.Solicitud.Activo,
            Observaciones = observaciones
        };

        await _unitOfWork.Repository<VehiculoFlota>().AddAsync(vehiculoNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoPlacaException(placa);
        }

        return vehiculoNuevo.Id;
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
