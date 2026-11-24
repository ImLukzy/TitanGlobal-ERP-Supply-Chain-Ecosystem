namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public class NotaCreditoDebitoListadoDto
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateOnly? FechaEmision { get; set; }
}
