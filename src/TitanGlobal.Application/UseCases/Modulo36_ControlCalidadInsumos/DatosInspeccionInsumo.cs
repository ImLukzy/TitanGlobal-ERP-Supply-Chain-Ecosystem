using TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo36_ControlCalidadInsumos;

// Campos del alta y de la edición, validados en el orden del DTO.
internal sealed class DatosInspeccionInsumo
{
    private DatosInspeccionInsumo(
        int productoId, int proveedorId, string lote, DateOnly fecha, decimal recibida, decimal muestra,
        string unidad, decimal tolerancia, string inspector)
    {
        ProductoId = productoId;
        ProveedorId = proveedorId;
        Lote = lote;
        Fecha = fecha;
        Recibida = recibida;
        Muestra = muestra;
        Unidad = unidad;
        Tolerancia = tolerancia;
        Inspector = inspector;
    }

    public int ProductoId { get; }

    public int ProveedorId { get; }

    public string Lote { get; }

    public DateOnly Fecha { get; }

    public decimal Recibida { get; }

    public decimal Muestra { get; }

    public string Unidad { get; }

    public decimal Tolerancia { get; }

    public string Inspector { get; }

    public static DatosInspeccionInsumo Validar(GuardarInspeccionInsumoRequestDto solicitud)
    {
        ValidacionInspeccionInsumo.ValidarIdProducto(solicitud.ProductoId);
        ValidacionInspeccionInsumo.ValidarIdProveedor(solicitud.ProveedorId);
        string lote = ValidacionInspeccionInsumo.NormalizarLote(solicitud.Lote);
        DateOnly fecha = ValidacionInspeccionInsumo.ValidarFechaInspeccion(solicitud.FechaInspeccion);
        decimal recibida = ValidacionInspeccionInsumo.ValidarCantidadRecibida(solicitud.CantidadRecibida);
        decimal muestra = ValidacionInspeccionInsumo.ValidarCantidadMuestra(solicitud.CantidadMuestra, recibida);
        string unidad = ValidacionInspeccionInsumo.NormalizarUnidad(solicitud.UnidadMedida);
        decimal tolerancia = ValidacionInspeccionInsumo.ValidarTolerancia(solicitud.ToleranciaDefectosPorcentaje);
        string inspector = ValidacionInspeccionInsumo.NormalizarInspector(solicitud.Inspector);
        return new DatosInspeccionInsumo(
            solicitud.ProductoId, solicitud.ProveedorId, lote, fecha, recibida, muestra, unidad, tolerancia, inspector);
    }

    public void Aplicar(InspeccionInsumo inspeccion)
    {
        inspeccion.ProductoId = ProductoId;
        inspeccion.ProveedorId = ProveedorId;
        inspeccion.Lote = Lote;
        inspeccion.FechaInspeccion = Fecha;
        inspeccion.CantidadRecibida = Recibida;
        inspeccion.CantidadMuestra = Muestra;
        inspeccion.UnidadMedida = Unidad;
        inspeccion.ToleranciaDefectosPorcentaje = Tolerancia;
        inspeccion.Inspector = Inspector;
    }
}
