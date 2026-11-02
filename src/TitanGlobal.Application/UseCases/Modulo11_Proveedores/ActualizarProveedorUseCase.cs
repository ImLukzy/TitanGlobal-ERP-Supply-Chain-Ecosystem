using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

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
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El proveedor {id} no existe.");
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
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoRucException(ruc);
        }

        return proveedor.Id;
    }

    private static bool EsViolacionUnica(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }

    private static ReglaNegocioException CrearConflictoRucException(string ruc)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un proveedor con el RUC {ruc}.");
    }
}
