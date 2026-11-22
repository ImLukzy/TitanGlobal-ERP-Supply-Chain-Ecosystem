using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class AnularSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public AnularSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, AnularSiniestroRequestDto datos)
    {
        SiniestroFlota siniestro = await ValidacionSiniestro.BuscarAsync(_unidad, id);
        if (siniestro.Estado == "Anulado")
        {
            throw ValidacionSiniestro.Conflicto("El siniestro ya está anulado.");
        }
        if (siniestro.Estado != "Registrado" && siniestro.Estado != "EnReclamo")
        {
            throw ValidacionSiniestro.Conflicto("Solo se puede anular un siniestro registrado o en reclamo.");
        }
        string motivo = datos.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionSiniestro.Error("El motivo de anulación es obligatorio.");
        }
        if (motivo.Length > 500)
        {
            throw ValidacionSiniestro.Error("El motivo de anulación no puede superar 500 caracteres.");
        }
        siniestro.MotivoAnulacion = motivo;
        siniestro.FechaAnulacion = FechasUtc.Normalizar(DateTime.UtcNow);
        siniestro.Estado = "Anulado";
        await _unidad.CompleteAsync();
        return id;
    }
}
