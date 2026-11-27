namespace TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;

public class ConsolidacionListadoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int TotalUnidades { get; set; }
    public int UnidadesRecogidas { get; set; }
    public int TotalBultos { get; set; }
    public string VolumenM3 { get; set; } = string.Empty;
    public string PesoKg { get; set; } = string.Empty;
}
