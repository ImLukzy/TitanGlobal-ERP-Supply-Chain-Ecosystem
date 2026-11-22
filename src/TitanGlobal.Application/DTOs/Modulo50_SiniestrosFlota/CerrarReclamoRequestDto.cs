namespace TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;

public class CerrarReclamoRequestDto
{
    public string Resultado { get; set; } = string.Empty;
    public decimal MontoReconocido { get; set; }
    public string Resolucion { get; set; } = string.Empty;
}
