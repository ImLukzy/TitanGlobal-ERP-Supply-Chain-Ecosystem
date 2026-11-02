using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

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
        catch (ViolacionRestriccionException excepcion) when (EsViolacionUnica(excepcion))
        {
            throw CrearConflictoRucException(ruc);
        }

        return proveedorNuevo.Id;
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
