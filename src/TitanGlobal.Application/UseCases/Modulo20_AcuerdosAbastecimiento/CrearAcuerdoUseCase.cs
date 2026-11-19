using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo20_AcuerdosAbastecimiento;

public class CrearAcuerdoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearAcuerdoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearAcuerdoRequestDto solicitud)
    {
        string numero = ValidacionAcuerdoAbastecimiento.NormalizarNumero(solicitud.Numero);
        if (solicitud.ProveedorId <= 0)
        {
            throw ValidacionAcuerdoAbastecimiento.Error("El proveedor es obligatorio.");
        }

        bool? proveedorActivo = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Where(proveedor => proveedor.Id == solicitud.ProveedorId)
            .Select(proveedor => (bool?)proveedor.Activo)
            .SingleOrDefaultAsync();
        if (proveedorActivo is null)
        {
            throw ValidacionAcuerdoAbastecimiento.Error($"El proveedor {solicitud.ProveedorId} no existe.");
        }

        if (!proveedorActivo.Value)
        {
            throw ValidacionAcuerdoAbastecimiento.Error($"El proveedor {solicitud.ProveedorId} está inactivo.");
        }

        string? descripcion = ValidacionAcuerdoAbastecimiento.NormalizarDescripcion(solicitud.Descripcion);
        ValidacionAcuerdoAbastecimiento.ValidarMontoMaximo(solicitud.MontoMaximo);
        ValidacionAcuerdoAbastecimiento.ValidarPeriodo(
            solicitud.FechaInicio,
            solicitud.FechaFin,
            out DateTime fechaInicio,
            out DateTime fechaFin);

        bool numeroExiste = await _unitOfWork.Repository<AcuerdoAbastecimiento>()
            .AsQueryable()
            .AnyAsync(acuerdo => acuerdo.Numero == numero);
        if (numeroExiste)
        {
            throw CrearConflictoNumero(numero);
        }

        AcuerdoAbastecimiento acuerdoNuevo = new AcuerdoAbastecimiento
        {
            Numero = numero,
            ProveedorId = solicitud.ProveedorId,
            Descripcion = descripcion,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            MontoMaximo = solicitud.MontoMaximo,
            Estado = ValidacionAcuerdoAbastecimiento.Vigente,
            FechaRegistro = DateTime.UtcNow
        };
        await _unitOfWork.Repository<AcuerdoAbastecimiento>().AddAsync(acuerdoNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoNumero(numero);
        }

        return acuerdoNuevo.Id;
    }

    private static ReglaNegocioException CrearConflictoNumero(string numero)
    {
        return ValidacionAcuerdoAbastecimiento.Conflicto($"Ya existe un acuerdo con el número {numero}.");
    }
}
