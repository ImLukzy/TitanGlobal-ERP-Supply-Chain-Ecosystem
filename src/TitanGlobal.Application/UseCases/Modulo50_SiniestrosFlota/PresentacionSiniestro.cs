using System.Globalization;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Domain.Models;
namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

internal static class PresentacionSiniestro
{
    public static SiniestroListadoDto Listado(SiniestroFlota siniestro)
    {
        SiniestroListadoDto dto = new SiniestroListadoDto();
        Asignar(siniestro, dto);
        return dto;
    }
    public static SiniestroDetalleDto Detalle(SiniestroFlota siniestro)
    {
        SiniestroDetalleDto dto = new SiniestroDetalleDto
        {
            Observaciones = siniestro.Observaciones,
            NumeroPoliza = siniestro.NumeroPoliza,
            Aseguradora = siniestro.Aseguradora,
            NumeroReclamo = siniestro.NumeroReclamo,
            Moneda = siniestro.Moneda,
            Resultado = siniestro.Resultado,
            Resolucion = siniestro.Resolucion,
            MotivoAnulacion = siniestro.MotivoAnulacion,
            FechaReclamo = siniestro.FechaReclamo,
            FechaCierre = siniestro.FechaCierre,
            FechaAnulacion = siniestro.FechaAnulacion,
            MontoReclamado = siniestro.MontoReclamado?.ToString("F2", CultureInfo.InvariantCulture),
            MontoReconocido = siniestro.MontoReconocido?.ToString("F2", CultureInfo.InvariantCulture)
        };
        Asignar(siniestro, dto);
        return dto;
    }
    private static void Asignar(SiniestroFlota siniestro, SiniestroListadoDto dto)
    {
        dto.Codigo = siniestro.Codigo;
        dto.Placa = siniestro.Placa;
        dto.Lugar = siniestro.Lugar;
        dto.Descripcion = siniestro.Descripcion;
        dto.Tipo = siniestro.Tipo;
        dto.Id = siniestro.Id;
        dto.Estado = siniestro.Estado;
        dto.FechaIncidente = siniestro.FechaIncidente;
    }
}
