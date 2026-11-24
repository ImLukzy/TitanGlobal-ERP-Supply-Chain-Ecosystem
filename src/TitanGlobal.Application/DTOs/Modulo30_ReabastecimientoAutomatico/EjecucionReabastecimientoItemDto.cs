namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class EjecucionReabastecimientoItemDto
{
    public int Id { get; set; }

    public int ReglaReabastecimientoId { get; set; }

    public string ReglaCodigo { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public string AlmacenNombre { get; set; } = string.Empty;

    public int? ProveedorPreferidoId { get; set; }

    public string? ProveedorPreferidoNombre { get; set; }

    public DateTime FechaEjecucion { get; set; }

    public int StockActual { get; set; }

    public int PuntoReorden { get; set; }

    public int CantidadSugerida { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int? SolicitudCompraId { get; set; }

    public string? SolicitudCompraNumero { get; set; }

    public string? Observacion { get; set; }
}
