namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class ReglaReabastecimientoListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public decimal ProductoPrecio { get; set; }

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public string AlmacenNombre { get; set; } = string.Empty;

    public int? ProveedorPreferidoId { get; set; }

    public string? ProveedorPreferidoNombre { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; }

    public int PuntoReorden { get; set; }

    public int StockMaximo { get; set; }

    public int CantidadLoteCompra { get; set; }

    public int CantidadSugeridaActual { get; set; }

    public bool RequiereReabastecimiento { get; set; }

    public bool Activa { get; set; }

    public DateTime? UltimaEvaluacion { get; set; }
}
