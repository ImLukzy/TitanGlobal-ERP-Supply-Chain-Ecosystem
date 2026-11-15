namespace TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;

public class LineaKardexDto
{
    public int MovimientoId { get; set; }

    public DateTime Fecha { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal ValorMovimiento { get; set; }

    public int SaldoCantidad { get; set; }

    public decimal SaldoValor { get; set; }

    public decimal CostoPromedio { get; set; }
}
