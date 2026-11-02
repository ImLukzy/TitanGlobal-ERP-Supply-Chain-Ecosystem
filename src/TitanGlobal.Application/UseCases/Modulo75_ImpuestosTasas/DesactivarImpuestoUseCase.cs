using System;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas;

public class DesactivarImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public DesactivarImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Ejecutar(int id)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (impuesto == null)
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        impuesto.Activo = false;

        _unitOfWork.Repository<Impuesto>().Update(impuesto);
        await _unitOfWork.CompleteAsync();
    }
}
