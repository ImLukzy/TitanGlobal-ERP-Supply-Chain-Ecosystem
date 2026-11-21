using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

public class CompletarEntregaUseCase
{
    private readonly IUnitOfWork _unidad;

    public CompletarEntregaUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> ExecuteAsync(int id, RegistrarPodRequestDto solicitud)
    {
        EntregaDespacho entrega = await ValidacionEntrega.ObtenerAsync(_unidad, id);
        if (entrega.Estado != "EnRuta")
        {
            throw ValidacionEntrega.Conflicto("Solo se puede completar una entrega en ruta.");
        }
        RegistrarPodRequestDto datos = ValidacionEntrega.NormalizarPod(solicitud);
        DateTime fecha = FechasUtc.Normalizar(DateTime.UtcNow);
        entrega.Estado = "Entregada";
        entrega.FechaEntrega = fecha;
        entrega.Receptor = datos.Receptor;
        entrega.DocumentoReceptor = datos.DocumentoReceptor;
        entrega.ReferenciaConstancia = datos.ReferenciaConstancia;
        entrega.ObservacionesEntrega = datos.Observaciones;
        await ValidacionEntrega.GuardarTransicionAsync(_unidad, entrega, fecha, "Recepción registrada mediante constancia manual.");
        return id;
    }
}
