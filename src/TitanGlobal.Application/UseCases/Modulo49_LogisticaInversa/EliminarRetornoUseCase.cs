using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class EliminarRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public EliminarRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task ExecuteAsync(int id)
    {
        RetornoLogistico retorno = await ValidacionRetorno.BuscarAsync(_unidad, id);
        if (retorno.Estado != "Programado")
        {
            throw ValidacionRetorno.Conflicto("Solo se puede eliminar un retorno programado.");
        }
        _unidad.Repository<RetornoLogistico>().Remove(retorno);
        try
        {
            await _unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw ValidacionRetorno.Conflicto("No se puede eliminar el retorno porque está en uso.");
        }
    }
}
