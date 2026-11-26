using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

public class EliminarConsolidacionUseCase
{
    private readonly IUnitOfWork _unidad;
    public EliminarConsolidacionUseCase(IUnitOfWork unidad)
    { _unidad = unidad; }
    public async Task<int> ExecuteAsync(int id)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, id);
        ValidacionCarga.ExigirBorrador(carga, "eliminar");
        _unidad.Repository<ConsolidacionCarga>().Remove(carga);
        try { await _unidad.CompleteAsync(); }
        catch (ViolacionRestriccionException error) when (error.Tipo == TipoViolacionRestriccion.ReferenciaForanea) { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar la consolidación porque está en uso."); }
        return id;
    }
}
