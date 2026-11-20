namespace TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;

public class SaldoKardexDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int AlmacenId { get; set; }

    public string AlmacenCodigo { get; set; } = string.Empty;

    public int Entradas { get; set; }

    public int Salidas { get; set; }

    public int Saldo { get; set; }
}
