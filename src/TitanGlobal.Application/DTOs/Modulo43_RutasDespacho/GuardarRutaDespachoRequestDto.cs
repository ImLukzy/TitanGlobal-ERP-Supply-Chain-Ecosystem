namespace TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;

public class GuardarRutaDespachoRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Zona { get; set; } = string.Empty;

    public string Origen { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public decimal DistanciaKm { get; set; }

    public decimal PeajeEstimado { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public string? Observaciones { get; set; }
}
