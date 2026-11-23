namespace TitanGlobal.Application.DTOs.Modulo28_TomaInventario;

public class DetalleTomaInventarioDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string ProductoCodigo { get; set; } = string.Empty;

    public string ProductoNombre { get; set; } = string.Empty;

    public int StockSistema { get; set; }

    public int CantidadContada { get; set; }

    public int Diferencia { get; set; }

    public string? Observacion { get; set; }
}
