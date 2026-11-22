namespace TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;

public class SiniestroListadoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string Lugar { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime FechaIncidente { get; set; }
    public string Estado { get; set; } = string.Empty;
}
