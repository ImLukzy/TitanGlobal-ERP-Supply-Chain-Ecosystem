using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo11_Proveedores.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.UseCases;

public class CrearProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarProveedorRequestDto solicitud)
    {
        string razonSocial = ValidacionProveedor.NormalizarRazonSocial(solicitud.RazonSocial);
        string ruc = ValidacionProveedor.NormalizarRuc(solicitud.Ruc);
        string pais = ValidacionProveedor.NormalizarPais(solicitud.Pais);
        string? email = ValidacionProveedor.NormalizarEmail(solicitud.Email);
        string? telefono = ValidacionProveedor.NormalizarTelefono(solicitud.Telefono);
        bool rucExiste = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .AnyAsync(proveedor => proveedor.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        Proveedor proveedorNuevo = new Proveedor
        {
            RazonSocial = razonSocial,
            Ruc = ruc,
            Pais = pais,
            Email = email,
            Telefono = telefono,
            Activo = solicitud.Activo
        };
        await _unitOfWork.Repository<Proveedor>().AddAsync(proveedorNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoRucException(ruc);
        }

        return proveedorNuevo.Id;
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        return excepcion.InnerException is PostgresException excepcionPostgres
            && excepcionPostgres.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static ReglaNegocioException CrearConflictoRucException(string ruc)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe un proveedor con el RUC {ruc}.");
    }
}
