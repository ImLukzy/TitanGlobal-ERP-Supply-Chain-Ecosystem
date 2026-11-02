using System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

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
                StatusCodes.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        impuesto.Activo = false;

        _unitOfWork.Repository<Impuesto>().Update(impuesto);
        await _unitOfWork.CompleteAsync();
    }
}
