using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga;

public class CancelarConsolidacionUseCase
{
    private readonly IUnitOfWork _unidad;
    public CancelarConsolidacionUseCase(IUnitOfWork unidad)
    { _unidad = unidad; }
    public async Task<int> ExecuteAsync(int id, CancelarConsolidacionRequestDto solicitud)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, id);
        if (carga.Estado == "Cancelada") { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La consolidación ya está cancelada."); }
        string motivo = ValidacionCarga.Texto(solicitud.Motivo, 500, "El motivo de cancelación es obligatorio.", "El motivo de cancelación no puede superar 500 caracteres.");
        carga.Estado = "Cancelada"; carga.MotivoCancelacion = motivo; carga.FechaCancelacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidad.Repository<ConsolidacionCarga>().Update(carga); await _unidad.CompleteAsync(); return id;
    }
}
