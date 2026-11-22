using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class CrearRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public CrearRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(GuardarRetornoRequestDto datos)
    {
        ValidacionRetorno.Validar(datos);
        await ValidacionRetorno.CodigoLibreAsync(_unidad, datos.Codigo);
        RetornoLogistico retorno = new RetornoLogistico { Estado = "Programado" };
        ValidacionRetorno.Asignar(retorno, datos);
        await _unidad.Repository<RetornoLogistico>().AddAsync(retorno);
        await ValidacionRetorno.GuardarAsync(_unidad, datos.Codigo);
        return retorno.Id;
    }
}
