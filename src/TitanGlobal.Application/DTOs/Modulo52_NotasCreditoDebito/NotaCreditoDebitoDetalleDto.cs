namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class NotaCreditoDebitoDetalleDto : NotaCreditoDebitoListadoDto
{
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string FacturaTipoComprobante { get; set; } = string.Empty;
    public string FacturaSerie { get; set; } = string.Empty;
    public int FacturaNumero { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}
