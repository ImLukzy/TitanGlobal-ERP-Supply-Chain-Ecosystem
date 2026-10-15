namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class CotizacionListadoDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime FechaValidez { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool Vencida { get; set; }
    public decimal Total { get; set; }
    public string? OrdenCompraNumero { get; set; }
}
