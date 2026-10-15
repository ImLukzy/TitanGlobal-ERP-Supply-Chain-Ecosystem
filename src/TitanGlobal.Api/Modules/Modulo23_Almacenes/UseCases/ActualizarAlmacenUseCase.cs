using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo23_Almacenes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.UseCases;

public class ActualizarAlmacenUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarAlmacenUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, GuardarAlmacenRequestDto solicitud)
    {
        Almacen? almacen = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (almacen is null)
        {
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El almacén {id} no existe.");
        }

        string codigo = ValidacionAlmacen.NormalizarCodigo(solicitud.Codigo);
        string nombre = ValidacionAlmacen.NormalizarNombre(solicitud.Nombre);
        string tipo = ValidacionAlmacen.NormalizarTipo(solicitud.Tipo);
        string direccion = ValidacionAlmacen.NormalizarDireccion(solicitud.Direccion);
        string ciudad = ValidacionAlmacen.NormalizarCiudad(solicitud.Ciudad);
        decimal capacidadM3 = ValidacionAlmacen.ValidarCapacidad(solicitud.CapacidadM3);
        string? responsable = ValidacionAlmacen.NormalizarResponsable(solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != id && entidad.Codigo == codigo);
        if (codigoExiste)
        {
            throw PersistenciaAlmacen.CrearConflictoCodigo(codigo);
        }

        almacen.Codigo = codigo;
        almacen.Nombre = nombre;
        almacen.Tipo = tipo;
        almacen.Direccion = direccion;
        almacen.Ciudad = ciudad;
        almacen.CapacidadM3 = capacidadM3;
        almacen.Responsable = responsable;
        almacen.Activo = solicitud.Activo;
        _unitOfWork.Repository<Almacen>().Update(almacen);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DbUpdateException excepcion) when (PersistenciaAlmacen.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaAlmacen.CrearConflictoCodigo(codigo);
        }

        return almacen.Id;
    }
}
