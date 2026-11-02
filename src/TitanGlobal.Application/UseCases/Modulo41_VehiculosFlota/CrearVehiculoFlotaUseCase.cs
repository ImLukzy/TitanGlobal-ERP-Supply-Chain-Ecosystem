using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota;

public class CrearVehiculoFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearVehiculoFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarVehiculoFlotaRequestDto solicitud)
    {
        string placa = ValidacionVehiculoFlota.NormalizarPlaca(solicitud.Placa);
        string marca = ValidacionVehiculoFlota.NormalizarMarca(solicitud.Marca);
        string modelo = ValidacionVehiculoFlota.NormalizarModelo(solicitud.Modelo);
        int anio = ValidacionVehiculoFlota.ValidarAnio(solicitud.Anio);
        string tipo = ValidacionVehiculoFlota.NormalizarTipo(solicitud.Tipo);
        decimal capacidadKg = ValidacionVehiculoFlota.ValidarCapacidadKg(solicitud.CapacidadKg);
        string? observaciones = ValidacionVehiculoFlota.NormalizarObservaciones(solicitud.Observaciones);

        bool placaExiste = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .AnyAsync(vehiculo => vehiculo.Placa == placa);

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
            Activo = solicitud.Activo,
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
