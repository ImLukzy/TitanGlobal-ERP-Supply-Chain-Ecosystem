using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

public class ObtenerRetornoUseCase
{
    private readonly IUnitOfWork _unidad;
    public ObtenerRetornoUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<RetornoDetalleDto> ExecuteAsync(int id)
    {
        RetornoLogistico retorno = await ValidacionRetorno.BuscarAsync(_unidad, id);
        return PresentacionRetorno.Detalle(retorno);
    }
}
