using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes.Commands;

public class CrearAlmacenCommand : IRequest<int>
{
    public required GuardarAlmacenRequestDto Solicitud { get; set; }
}

public class CrearAlmacenCommandHandler : IRequestHandler<CrearAlmacenCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearAlmacenCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearAlmacenCommand request, CancellationToken cancellationToken)
    {
        string codigo = ValidacionAlmacen.NormalizarCodigo(request.Solicitud.Codigo);
        string nombre = ValidacionAlmacen.NormalizarNombre(request.Solicitud.Nombre);
        string tipo = ValidacionAlmacen.NormalizarTipo(request.Solicitud.Tipo);
        string direccion = ValidacionAlmacen.NormalizarDireccion(request.Solicitud.Direccion);
        string ciudad = ValidacionAlmacen.NormalizarCiudad(request.Solicitud.Ciudad);
        decimal capacidadM3 = ValidacionAlmacen.ValidarCapacidad(request.Solicitud.CapacidadM3);
        string? responsable = ValidacionAlmacen.NormalizarResponsable(request.Solicitud.Responsable);
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
            Activo = request.Solicitud.Activo
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
