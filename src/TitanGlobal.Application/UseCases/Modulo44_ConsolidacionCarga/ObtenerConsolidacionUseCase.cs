using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

public class ObtenerConsolidacionUseCase
{
    private readonly IUnitOfWork _unidad;
    public ObtenerConsolidacionUseCase(IUnitOfWork unidad)
    { _unidad = unidad; }
    public async Task<ConsolidacionDetalleDto> ExecuteAsync(int id)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, id);
        return CalculoCarga.Detalle(carga);
    }
}
