namespace TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;

public class RegistrarReclamoRequestDto
{
    public string NumeroPoliza { get; set; } = string.Empty;
    public string Aseguradora { get; set; } = string.Empty;
    public string NumeroReclamo { get; set; } = string.Empty;
    public string Moneda { get; set; } = string.Empty;
    public decimal MontoReclamado { get; set; }
}
