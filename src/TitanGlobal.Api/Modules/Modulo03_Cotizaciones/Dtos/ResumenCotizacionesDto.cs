namespace TitanGlobal.Api.Modules.Modulo03_Cotizaciones.Dtos;

public class ResumenCotizacionesDto
{
    public int Total { get; set; }
    public int Emitidas { get; set; }
    public int Vencidas { get; set; }
    public int Aceptadas { get; set; }
    public int Rechazadas { get; set; }
    public decimal MontoVigente { get; set; }
    public decimal MontoAceptado { get; set; }
    public decimal TasaAceptacion { get; set; }
    public List<ClienteCotizacionesDto> TopClientes { get; set; } = new List<ClienteCotizacionesDto>();
}
