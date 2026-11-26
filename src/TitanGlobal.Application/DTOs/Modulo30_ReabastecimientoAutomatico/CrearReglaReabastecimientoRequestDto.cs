namespace TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;

public class CrearReglaReabastecimientoRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public int AlmacenId { get; set; }

    public int? ProveedorPreferidoId { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; }

    public int PuntoReorden { get; set; }

    public int StockMaximo { get; set; }

    public int CantidadLoteCompra { get; set; } = 1;

    public bool Activa { get; set; } = true;

    public string? Observaciones { get; set; }
}
