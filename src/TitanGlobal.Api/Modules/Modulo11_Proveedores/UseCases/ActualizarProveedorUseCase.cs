using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo11_Proveedores.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.UseCases;

public class ActualizarProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarProveedorRequestDto solicitud)
    {
        Proveedor? proveedor = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (proveedor is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El proveedor {id} no existe.");
        }

        string razonSocial = ValidacionProveedor.NormalizarRazonSocial(solicitud.RazonSocial);
        string ruc = ValidacionProveedor.NormalizarRuc(solicitud.Ruc);
        string pais = ValidacionProveedor.NormalizarPais(solicitud.Pais);
        string? email = ValidacionProveedor.NormalizarEmail(solicitud.Email);
        string? telefono = ValidacionProveedor.NormalizarTelefono(solicitud.Telefono);
        bool rucExiste = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != id && entidad.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        proveedor.RazonSocial = razonSocial;
        proveedor.Ruc = ruc;
        proveedor.Pais = pais;
        proveedor.Email = email;
        proveedor.Telefono = telefono;
        proveedor.Activo = solicitud.Activo;
        _unitOfWork.Repository<Proveedor>().Update(proveedor);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoRucException(ruc);
        }

        return proveedor.Id;
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
