using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class CancelarEntregaUseCase
{
    private readonly IUnitOfWork _unidad;

    public CancelarEntregaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, CancelarEntregaRequestDto solicitud)
    {
        EntregaDespacho entrega = await ValidacionEntrega.ObtenerAsync(_unidad, id);
        if (entrega.Estado == "Cancelada")
        {
            throw ValidacionEntrega.Conflicto("La entrega ya está cancelada.");
        }
        if (entrega.Estado != "Programada" && entrega.Estado != "EnRuta")
        {
            throw ValidacionEntrega.Conflicto("Solo se puede cancelar una entrega programada o en ruta.");
        }
        string motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw ValidacionEntrega.Error("El motivo de cancelación es obligatorio.");
        }
        if (motivo.Length > 500)
        {
            throw ValidacionEntrega.Error("El motivo de cancelación no puede superar 500 caracteres.");
        }
        DateTime fecha = FechasUtc.Normalizar(DateTime.UtcNow);
        entrega.Estado = "Cancelada";
        entrega.FechaCancelacion = fecha;
        entrega.MotivoCancelacion = motivo;
        await ValidacionEntrega.GuardarTransicionAsync(_unidad, entrega, fecha, motivo);
        return id;
    }
}
