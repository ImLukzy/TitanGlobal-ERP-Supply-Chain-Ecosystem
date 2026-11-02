using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo41_VehiculosFlota.UseCases;

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
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoPlacaException(placa);
        }

        return vehiculoNuevo.Id;
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
