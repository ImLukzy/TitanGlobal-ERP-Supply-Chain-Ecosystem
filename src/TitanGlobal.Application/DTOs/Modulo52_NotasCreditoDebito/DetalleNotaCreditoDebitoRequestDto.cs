namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class DetalleNotaCreditoDebitoRequestDto
{
    public int DetalleFacturaId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
