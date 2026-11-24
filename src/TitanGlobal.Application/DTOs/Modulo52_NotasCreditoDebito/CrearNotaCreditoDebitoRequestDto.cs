namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class CrearNotaCreditoDebitoRequestDto
{
    public int FacturaId { get; set; }
    public string? Tipo { get; set; }
    public string? Motivo { get; set; }
    public List<DetalleNotaCreditoDebitoRequestDto?>? Detalles { get; set; }
}
