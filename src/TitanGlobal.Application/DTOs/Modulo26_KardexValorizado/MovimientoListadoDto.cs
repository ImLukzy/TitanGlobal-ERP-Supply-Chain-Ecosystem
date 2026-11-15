namespace TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;

public class MovimientoListadoDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public int? LoteId { get; set; }

    public DateTime Fecha { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal? CostoUnitario { get; set; }

    public string? Referencia { get; set; }
}
