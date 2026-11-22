using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

public class ReclamarSiniestroUseCase
{
    private readonly IUnitOfWork _unidad;
    public ReclamarSiniestroUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }
    public async Task<int> ExecuteAsync(int id, RegistrarReclamoRequestDto datos)
    {
        SiniestroFlota siniestro = await ValidacionSiniestro.BuscarAsync(_unidad, id);
        if (siniestro.Estado != "Registrado")
        {
            throw ValidacionSiniestro.Conflicto("Solo se puede reclamar un siniestro registrado.");
        }
        string poliza = ValidacionSiniestro.Texto(datos.NumeroPoliza, "número de póliza", 40);
        string aseguradora = ValidacionSiniestro.Texto(datos.Aseguradora, "aseguradora", 150);
        string reclamo = ValidacionSiniestro.Texto(datos.NumeroReclamo, "número de reclamo", 40);
        string moneda = datos.Moneda?.Trim().ToUpperInvariant() ?? string.Empty;
        if (moneda.Length != 3 || moneda.Any(c => c < 'A' || c > 'Z'))
        {
            throw ValidacionSiniestro.Error("La moneda debe contener 3 letras mayúsculas.");
        }
        ValidacionSiniestro.Monto(datos.MontoReclamado, "monto reclamado", 0.01m);
        siniestro.NumeroPoliza = poliza;
        siniestro.Aseguradora = aseguradora;
        siniestro.NumeroReclamo = reclamo;
        siniestro.Moneda = moneda;
        siniestro.MontoReclamado = datos.MontoReclamado;
        siniestro.FechaReclamo = FechasUtc.Normalizar(DateTime.UtcNow);
        siniestro.Estado = "EnReclamo";
        await _unidad.CompleteAsync();
        return id;
    }
}
