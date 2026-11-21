using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo46_MonitoreoEntregas;

internal static class PresentacionEntrega
{
    public static EntregaDetalleDto Detalle(EntregaDespacho entrega)
    {
        return new EntregaDetalleDto
        {
            Id = entrega.Id,
            Codigo = entrega.Codigo,
            ReferenciaGuia = entrega.ReferenciaGuia,
            Destinatario = entrega.Destinatario,
            DireccionDestino = entrega.DireccionDestino,
            FechaProgramada = entrega.FechaProgramada,
            Estado = entrega.Estado,
            Observaciones = entrega.Observaciones,
            FechaSalida = entrega.FechaSalida,
            FechaEntrega = entrega.FechaEntrega,
            FechaCancelacion = entrega.FechaCancelacion,
            Receptor = entrega.Receptor,
            DocumentoReceptor = entrega.DocumentoReceptor,
            ReferenciaConstancia = entrega.ReferenciaConstancia,
            ObservacionesEntrega = entrega.ObservacionesEntrega,
            MotivoCancelacion = entrega.MotivoCancelacion,
            Historial = entrega.Eventos.OrderBy(e => e.Id).Select(e => new EventoEntregaDto
            {
                Id = e.Id,
                Estado = e.Estado,
                Fecha = e.Fecha,
                Descripcion = e.Descripcion
            }).ToList()
        };
    }
}
