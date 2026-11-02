using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo41_VehiculosFlota;

public class ActualizarVehiculoFlotaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarVehiculoFlotaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarVehiculoFlotaRequestDto solicitud)
    {
        string placa = ValidacionVehiculoFlota.NormalizarPlaca(solicitud.Placa);
        string marca = ValidacionVehiculoFlota.NormalizarMarca(solicitud.Marca);
        string modelo = ValidacionVehiculoFlota.NormalizarModelo(solicitud.Modelo);
        int anio = ValidacionVehiculoFlota.ValidarAnio(solicitud.Anio);
        string tipo = ValidacionVehiculoFlota.NormalizarTipo(solicitud.Tipo);
        decimal capacidadKg = ValidacionVehiculoFlota.ValidarCapacidadKg(solicitud.CapacidadKg);
        string? observaciones = ValidacionVehiculoFlota.NormalizarObservaciones(solicitud.Observaciones);

        VehiculoFlota? vehiculo = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (vehiculo is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El vehículo {id} no existe.");
        }

        bool placaEnUso = await _unitOfWork.Repository<VehiculoFlota>()
            .AsQueryable()
            .AnyAsync(v => v.Placa == placa && v.Id != id);

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
        vehiculo.Activo = solicitud.Activo;
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
