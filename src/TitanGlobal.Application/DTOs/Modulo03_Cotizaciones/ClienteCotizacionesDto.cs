namespace TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;

public class ClienteCotizacionesDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cotizaciones { get; set; }
    public decimal MontoAceptado { get; set; }
}
