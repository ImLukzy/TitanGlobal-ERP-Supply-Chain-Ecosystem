using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Commands;

public class ActualizarProveedorCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarProveedorRequestDto Solicitud { get; set; }
}

public class ActualizarProveedorCommandHandler : IRequestHandler<ActualizarProveedorCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarProveedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarProveedorCommand request, CancellationToken cancellationToken)
    {
        Proveedor? proveedor = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (proveedor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El proveedor {request.Id} no existe.");
        }

        string razonSocial = ValidacionProveedor.NormalizarRazonSocial(request.Solicitud.RazonSocial);
        string ruc = ValidacionProveedor.NormalizarRuc(request.Solicitud.Ruc);
        string pais = ValidacionProveedor.NormalizarPais(request.Solicitud.Pais);
        string? email = ValidacionProveedor.NormalizarEmail(request.Solicitud.Email);
        string? telefono = ValidacionProveedor.NormalizarTelefono(request.Solicitud.Telefono);
        bool rucExiste = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != request.Id && entidad.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        proveedor.RazonSocial = razonSocial;
        proveedor.Ruc = ruc;
        proveedor.Pais = pais;
        proveedor.Email = email;
        proveedor.Telefono = telefono;
        proveedor.Activo = request.Solicitud.Activo;
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
