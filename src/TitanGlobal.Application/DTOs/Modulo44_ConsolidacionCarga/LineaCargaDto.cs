namespace TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;

public class LineaCargaDto
{
    public int Id { get; set; }
    public int Posicion { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int CantidadRecogida { get; set; }
    public int UnidadesPorBulto { get; set; }
    public string LargoCm { get; set; } = string.Empty;
    public string AnchoCm { get; set; } = string.Empty;
    public string AltoCm { get; set; } = string.Empty;
    public string PesoBultoKg { get; set; } = string.Empty;
    public int Bultos { get; set; }
    public string VolumenM3 { get; set; } = string.Empty;
    public string PesoKg { get; set; } = string.Empty;
}
