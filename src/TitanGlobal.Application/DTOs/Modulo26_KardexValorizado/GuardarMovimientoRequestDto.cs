namespace TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;

public class GuardarMovimientoRequestDto
{
    public int ProductoId { get; set; }

    public int AlmacenId { get; set; }

    public int? LoteId { get; set; }

    public DateTime? Fecha { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal? CostoUnitario { get; set; }

    public string? Referencia { get; set; }
}
