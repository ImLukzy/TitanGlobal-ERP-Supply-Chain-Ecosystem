using MediatR;
using System;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas.Commands;

public class DesactivarImpuestoCommand : IRequest
{
    public int Id { get; set; }
}

public class DesactivarImpuestoCommandHandler : IRequestHandler<DesactivarImpuestoCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public DesactivarImpuestoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DesactivarImpuestoCommand request, CancellationToken cancellationToken)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (impuesto == null)
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        impuesto.Activo = false;

        _unitOfWork.Repository<Impuesto>().Update(impuesto);
        await _unitOfWork.CompleteAsync();
    }
}
