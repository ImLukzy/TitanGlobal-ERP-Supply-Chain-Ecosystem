namespace TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;

public class KardexDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public string Metodo { get; set; } = string.Empty;

    public int SaldoCantidad { get; set; }

    public decimal SaldoValor { get; set; }

    public List<LineaKardexDto> Lineas { get; set; } = new List<LineaKardexDto>();
}
