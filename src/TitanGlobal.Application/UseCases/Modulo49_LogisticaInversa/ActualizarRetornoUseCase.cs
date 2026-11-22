using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class ActualizarRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public ActualizarRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, GuardarRetornoRequestDto datos)
    {
        RetornoLogistico retorno = await ValidacionRetorno.BuscarAsync(_unidad, id);
        if (retorno.Estado != "Programado")
        {
            throw ValidacionRetorno.Conflicto("Solo se puede editar un retorno programado.");
        }
        ValidacionRetorno.Validar(datos);
        await ValidacionRetorno.CodigoLibreAsync(_unidad, datos.Codigo, id);
        ValidacionRetorno.Asignar(retorno, datos);
        await ValidacionRetorno.GuardarAsync(_unidad, datos.Codigo);
        return id;
    }
}
