namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class ActualizarNotaCreditoDebitoRequestDto
{
    public string? Motivo { get; set; }
    public List<DetalleNotaCreditoDebitoRequestDto?>? Detalles { get; set; }
}
