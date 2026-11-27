using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes.Commands;

public class ActualizarAlmacenCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarAlmacenRequestDto Solicitud { get; set; }
}

public class ActualizarAlmacenCommandHandler : IRequestHandler<ActualizarAlmacenCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarAlmacenCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarAlmacenCommand request, CancellationToken cancellationToken)
    {
        Almacen? almacen = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (almacen is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El almacén {request.Id} no existe.");
        }

        string codigo = ValidacionAlmacen.NormalizarCodigo(request.Solicitud.Codigo);
        string nombre = ValidacionAlmacen.NormalizarNombre(request.Solicitud.Nombre);
        string tipo = ValidacionAlmacen.NormalizarTipo(request.Solicitud.Tipo);
        string direccion = ValidacionAlmacen.NormalizarDireccion(request.Solicitud.Direccion);
        string ciudad = ValidacionAlmacen.NormalizarCiudad(request.Solicitud.Ciudad);
        decimal capacidadM3 = ValidacionAlmacen.ValidarCapacidad(request.Solicitud.CapacidadM3);
        string? responsable = ValidacionAlmacen.NormalizarResponsable(request.Solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<Almacen>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != request.Id && entidad.Codigo == codigo);
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
        almacen.Activo = request.Solicitud.Activo;
        _unitOfWork.Repository<Almacen>().Update(almacen);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaAlmacen.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaAlmacen.CrearConflictoCodigo(codigo);
        }

        return almacen.Id;
    }
}
