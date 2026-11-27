namespace TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;

public class RutaDespachoListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Zona { get; set; } = string.Empty;

    public string Origen { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public string DistanciaKm { get; set; } = string.Empty;

    public string PeajeEstimado { get; set; } = string.Empty;

    public string Moneda { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
