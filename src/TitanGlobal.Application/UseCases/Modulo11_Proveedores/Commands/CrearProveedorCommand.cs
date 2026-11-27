using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Commands;

public class CrearProveedorCommand : IRequest<int>
{
    public required GuardarProveedorRequestDto Solicitud { get; set; }
}

public class CrearProveedorCommandHandler : IRequestHandler<CrearProveedorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearProveedorCommand request, CancellationToken cancellationToken)
    {
        string razonSocial = ValidacionProveedor.NormalizarRazonSocial(request.Solicitud.RazonSocial);
        string ruc = ValidacionProveedor.NormalizarRuc(request.Solicitud.Ruc);
        string pais = ValidacionProveedor.NormalizarPais(request.Solicitud.Pais);
        string? email = ValidacionProveedor.NormalizarEmail(request.Solicitud.Email);
        string? telefono = ValidacionProveedor.NormalizarTelefono(request.Solicitud.Telefono);
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
            Activo = request.Solicitud.Activo
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
