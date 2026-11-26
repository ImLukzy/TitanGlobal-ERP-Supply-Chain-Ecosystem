using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo45_GuiasRemision;

internal static class PresentacionGuia
{
    public static GuiaListadoDto Listado(GuiaRemision guia)
    {
        return new GuiaListadoDto
        {
            Id = guia.Id,
            Serie = guia.Serie,
            Numero = guia.Numero,
            Estado = guia.Estado,
            FechaInicioTraslado = guia.FechaInicioTraslado,
            DestinatarioNombre = guia.DestinatarioNombre,
            DestinatarioDocumento = guia.DestinatarioDocumento,
            DireccionDestino = guia.DireccionDestino,
            PesoBrutoKg = guia.PesoBrutoKg.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            TotalLineas = guia.Lineas.Count,
        };
    }

    public static GuiaDetalleDto Detalle(GuiaRemision guia)
    {
        return new GuiaDetalleDto
        {
            Id = guia.Id,
            Serie = guia.Serie,
            Numero = guia.Numero,
            Estado = guia.Estado,
            FechaInicioTraslado = guia.FechaInicioTraslado,
            DestinatarioNombre = guia.DestinatarioNombre,
            DestinatarioDocumento = guia.DestinatarioDocumento,
            DireccionDestino = guia.DireccionDestino,
            PesoBrutoKg = guia.PesoBrutoKg.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            TotalLineas = guia.Lineas.Count,
            RemitenteNombre = guia.RemitenteNombre,
            RemitenteDocumento = guia.RemitenteDocumento,
            DireccionOrigen = guia.DireccionOrigen,
            MotivoTraslado = guia.MotivoTraslado,
            Transportista = guia.Transportista,
            Conductor = guia.Conductor,
            Placa = guia.Placa,
            Observaciones = guia.Observaciones,
            FechaEmision = guia.FechaEmision,
            FechaAnulacion = guia.FechaAnulacion,
            MotivoAnulacion = guia.MotivoAnulacion,
            Lineas = guia.Lineas.OrderBy(l => l.Posicion).ThenBy(l => l.Id).Select(l => new LineaGuiaDto
            {
                Id = l.Id,
                Posicion = l.Posicion,
                Sku = l.Sku,
                Descripcion = l.Descripcion,
                UnidadMedida = l.UnidadMedida,
                Cantidad = l.Cantidad.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
            }).ToList()
        };
    }
}
