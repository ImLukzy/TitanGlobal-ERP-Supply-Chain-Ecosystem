namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class ClienteCotizacionesDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cotizaciones { get; set; }
    public decimal MontoAceptado { get; set; }
}
