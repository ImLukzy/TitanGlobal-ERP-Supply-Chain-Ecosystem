namespace TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;

public class RegistrarAjusteInventarioRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public int ProductoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal? CostoUnitario { get; set; }

    public DateTime? Fecha { get; set; }

    public string Responsable { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
}
