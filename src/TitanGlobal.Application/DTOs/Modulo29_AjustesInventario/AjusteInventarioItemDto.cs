namespace TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;

public class AjusteInventarioItemDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public string AlmacenNombre { get; set; } = string.Empty;

    public int ProductoId { get; set; }

    public string ProductoCodigo { get; set; } = string.Empty;

    public string ProductoNombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal ValorTotal { get; set; }

    public DateTime Fecha { get; set; }

    public string Responsable { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}
