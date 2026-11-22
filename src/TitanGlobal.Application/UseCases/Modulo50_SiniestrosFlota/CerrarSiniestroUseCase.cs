using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class CerrarSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public CerrarSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, CerrarReclamoRequestDto datos)
    {
        SiniestroFlota siniestro = await ValidacionSiniestro.BuscarAsync(_unidad, id);
        if (siniestro.Estado != "EnReclamo")
        {
            throw ValidacionSiniestro.Conflicto("Solo se puede cerrar un siniestro en reclamo.");
        }
        string? resultado = new[] { "Aceptado", "Rechazado" }.FirstOrDefault(r => r.Equals(datos.Resultado?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (resultado is null)
        {
            throw ValidacionSiniestro.Error("El resultado debe ser Aceptado o Rechazado.");
        }
        ValidacionSiniestro.Monto(datos.MontoReconocido, "monto reconocido", 0);
        if (datos.MontoReconocido > siniestro.MontoReclamado!.Value)
        {
            throw ValidacionSiniestro.Error("El monto reconocido no puede superar el reclamado.");
        }
        if (resultado == "Aceptado" && datos.MontoReconocido == 0)
        {
            throw ValidacionSiniestro.Error("Un reclamo aceptado debe reconocer un importe mayor que cero.");
        }
        if (resultado == "Rechazado" && datos.MontoReconocido != 0)
        {
            throw ValidacionSiniestro.Error("Un reclamo rechazado debe reconocer un importe de cero.");
        }
        string resolucion = ValidacionSiniestro.Texto(datos.Resolucion, "resolución", 500);
        siniestro.Resultado = resultado;
        siniestro.MontoReconocido = datos.MontoReconocido;
        siniestro.Resolucion = resolucion;
        siniestro.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        siniestro.Estado = "Cerrado";
        await _unidad.CompleteAsync();
        return id;
    }
}
