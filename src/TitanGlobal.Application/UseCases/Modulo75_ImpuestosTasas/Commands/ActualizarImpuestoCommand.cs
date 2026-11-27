using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas.Commands;

public class ActualizarImpuestoCommand : IRequest
{
    public int Id { get; set; }

    public required GuardarImpuestoRequestDto Dto { get; set; }
}

public class ActualizarImpuestoCommandHandler : IRequestHandler<ActualizarImpuestoCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarImpuestoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ActualizarImpuestoCommand request, CancellationToken cancellationToken)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (impuesto == null)
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        impuesto.Codigo = request.Dto.Codigo;
        impuesto.Nombre = request.Dto.Nombre;
        impuesto.Tipo = request.Dto.Tipo;
        impuesto.Descripcion = request.Dto.Descripcion;
        impuesto.Pais = request.Dto.Pais;
        impuesto.Activo = request.Dto.Activo;

        ValidacionImpuesto.ValidarDatos(impuesto);

        bool codigoExiste = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .AnyAsync(x => x.Codigo == impuesto.Codigo && x.Id != request.Id);

        if (codigoExiste)
            throw CrearConflictoCodigoException(impuesto.Codigo);

        _unitOfWork.Repository<Impuesto>().Update(impuesto);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion)
            when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoCodigoException(impuesto.Codigo);
        }
    }

    private static ReglaNegocioException CrearConflictoCodigoException(
        string codigo)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un impuesto con el código {codigo}.");
    }
}
