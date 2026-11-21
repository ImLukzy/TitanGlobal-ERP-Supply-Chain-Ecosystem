using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

public class AnularGuiaUseCase
{
    private readonly IUnitOfWork _unidad;

    public AnularGuiaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, AnularGuiaRequestDto solicitud)
    {
        GuiaRemision guia = await ValidacionGuia.ObtenerAsync(_unidad, id);
        if (guia.Estado == "Anulada")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La guía ya está anulada.");
        }
        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionGuia.Error("El motivo de anulación es obligatorio.");
        }
        if (motivo.Length > 500)
        {
            throw ValidacionGuia.Error("El motivo de anulación no puede superar 500 caracteres.");
        }
        guia.Estado = "Anulada";
        guia.MotivoAnulacion = motivo;
        guia.FechaAnulacion = FechasUtc.Normalizar(DateTime.UtcNow);
        _unidad.Repository<GuiaRemision>().Update(guia);
        await _unidad.CompleteAsync();
        return id;
    }
}
