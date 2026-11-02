using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

public class ObtenerProveedorUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerProveedorUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProveedorDetalleDto> ExecuteAsync(int id)
    {
        ProveedorDetalleDto? proveedor = await _unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new ProveedorDetalleDto
            {
                Id = entidad.Id,
                RazonSocial = entidad.RazonSocial,
                Ruc = entidad.Ruc,
                Pais = entidad.Pais,
                Email = entidad.Email,
                Telefono = entidad.Telefono,
                Activo = entidad.Activo
            })
            .SingleOrDefaultAsync();

        if (proveedor is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El proveedor {id} no existe.");
        }

        return proveedor;
    }
}
