using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Domain.Models;
namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

internal static class PresentacionRetorno
{
    private static string? Decimal(decimal? valor)
    {
        return valor?.ToString("F3", CultureInfo.InvariantCulture);
    }
    public static RetornoListadoDto Listado(RetornoLogistico retorno)
    {
        RetornoListadoDto dto = new RetornoListadoDto();
        Asignar(retorno, dto);
        return dto;
    }
    public static RetornoDetalleDto Detalle(RetornoLogistico retorno)
    {
        RetornoDetalleDto dto = new RetornoDetalleDto
        {
            Observaciones = retorno.Observaciones,
            ResponsableRecogida = retorno.ResponsableRecogida,
            ResponsableRecepcion = retorno.ResponsableRecepcion,
            MotivoCancelacion = retorno.MotivoCancelacion,
            FechaRecogida = retorno.FechaRecogida,
            FechaRecepcion = retorno.FechaRecepcion,
            FechaCancelacion = retorno.FechaCancelacion,
            CantidadRecogida = Decimal(retorno.CantidadRecogida),
            CantidadRecibida = Decimal(retorno.CantidadRecibida),
            NoRecogida = retorno.CantidadRecogida.HasValue ? Decimal(retorno.CantidadProgramada - retorno.CantidadRecogida.Value) : null,
            NoRecibida = retorno.CantidadRecibida.HasValue ? Decimal(retorno.CantidadRecogida!.Value - retorno.CantidadRecibida.Value) : null
        };
        Asignar(retorno, dto);
        return dto;
    }
    private static void Asignar(RetornoLogistico retorno, RetornoListadoDto dto)
    {
        dto.Codigo = retorno.Codigo;
        dto.ReferenciaOrigen = retorno.ReferenciaOrigen;
        dto.Contraparte = retorno.Contraparte;
        dto.PuntoRecogida = retorno.PuntoRecogida;
        dto.PuntoRecepcion = retorno.PuntoRecepcion;
        dto.Descripcion = retorno.Descripcion;
        dto.Unidad = retorno.Unidad;
        dto.Id = retorno.Id;
        dto.Tipo = retorno.Tipo;
        dto.FechaProgramada = retorno.FechaProgramada;
        dto.Estado = retorno.Estado;
        dto.CantidadProgramada = Decimal(retorno.CantidadProgramada)!;
    }
}
