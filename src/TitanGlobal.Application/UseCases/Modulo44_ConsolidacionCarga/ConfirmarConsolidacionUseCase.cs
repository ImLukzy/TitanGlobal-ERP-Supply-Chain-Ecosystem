using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

public class ConfirmarConsolidacionUseCase
{
    private readonly IUnitOfWork _unidad;
    public ConfirmarConsolidacionUseCase(IUnitOfWork unidad)
    { _unidad = unidad; }
    public async Task<int> ExecuteAsync(int id)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, id);
        ValidacionCarga.ExigirBorrador(carga, "confirmar");
        if (carga.Lineas.Count == 0 || carga.Lineas.Any(l => l.CantidadRecogida != l.Cantidad)) { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "Completa el picking de todas las líneas antes de confirmar."); }
        if (carga.Lineas.Any(l => l.UnidadesPorBulto <= 0 || l.LargoCm <= 0 || l.AnchoCm <= 0 || l.AltoCm <= 0 || l.PesoBultoKg <= 0)) { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "Completa el packing de todas las líneas antes de confirmar."); }
        carga.Estado = "Confirmada"; carga.FechaConfirmacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidad.Repository<ConsolidacionCarga>().Update(carga); await _unidad.CompleteAsync(); return id;
    }
}
