using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes;

public class CrearAlmacenUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearAlmacenUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(GuardarAlmacenRequestDto solicitud)
    {
        string codigo = ValidacionAlmacen.NormalizarCodigo(solicitud.Codigo);
        string nombre = ValidacionAlmacen.NormalizarNombre(solicitud.Nombre);
        string tipo = ValidacionAlmacen.NormalizarTipo(solicitud.Tipo);
        string direccion = ValidacionAlmacen.NormalizarDireccion(solicitud.Direccion);
        string ciudad = ValidacionAlmacen.NormalizarCiudad(solicitud.Ciudad);
        decimal capacidadM3 = ValidacionAlmacen.ValidarCapacidad(solicitud.CapacidadM3);
        string? responsable = ValidacionAlmacen.NormalizarResponsable(solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .AnyAsync(almacen => almacen.Codigo == codigo);
        if (codigoExiste)
        {
            throw PersistenciaAlmacen.CrearConflictoCodigo(codigo);
        }

        Almacen almacenNuevo = new Almacen
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Direccion = direccion,
            Ciudad = ciudad,
            CapacidadM3 = capacidadM3,
            Responsable = responsable,
            Activo = solicitud.Activo
        };
        await _unitOfWork.Repository<Almacen>().AddAsync(almacenNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaAlmacen.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaAlmacen.CrearConflictoCodigo(codigo);
        }

        return almacenNuevo.Id;
    }
}
