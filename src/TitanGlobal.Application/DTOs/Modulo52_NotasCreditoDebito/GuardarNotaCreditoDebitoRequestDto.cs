namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class GuardarNotaCreditoDebitoRequestDto
{
    public int FacturaId { get; set; }
    public string? Tipo { get; set; }
    public string? Serie { get; set; }
    public int Numero { get; set; }
    public string? Motivo { get; set; }
    public decimal Importe { get; set; }
}
