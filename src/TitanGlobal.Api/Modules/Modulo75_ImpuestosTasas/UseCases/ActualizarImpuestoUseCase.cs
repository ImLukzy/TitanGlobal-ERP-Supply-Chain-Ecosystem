using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo75_ImpuestosTasas.UseCases;

public class ActualizarImpuestoUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarImpuestoUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Ejecutar(int id, GuardarImpuestoRequestDto dto)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (impuesto == null)
            throw new ReglaNegocioException(
                StatusCodes.Status404NotFound,
                "No se encontró el impuesto solicitado.");

        impuesto.Codigo = dto.Codigo;
        impuesto.Nombre = dto.Nombre;
        impuesto.Tipo = dto.Tipo;
        impuesto.Descripcion = dto.Descripcion;
        impuesto.Pais = dto.Pais;
        impuesto.Activo = dto.Activo;

        ValidacionImpuesto.ValidarDatos(impuesto);

        bool codigoExiste = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .AnyAsync(x => x.Codigo == impuesto.Codigo && x.Id != id);

        if (codigoExiste)
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El código ya pertenece a otro impuesto.");

        _unitOfWork.Repository<Impuesto>().Update(impuesto);
        await _unitOfWork.CompleteAsync();
    }
}
