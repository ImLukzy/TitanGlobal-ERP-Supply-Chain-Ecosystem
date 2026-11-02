using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El vehículo {id} no existe.");
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
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoPlacaException(placa);
        }

        return vehiculo.Id;
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        return excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static ReglaNegocioException CrearConflictoPlacaException(string placa)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe un vehículo con la placa {placa}.");
    }
}
