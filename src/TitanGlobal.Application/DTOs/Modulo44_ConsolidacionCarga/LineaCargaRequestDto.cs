namespace TitanGlobal.Application.DTOs.Modulo44_ConsolidacionCarga;

public class LineaCargaRequestDto
{
    public string Sku { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int CantidadRecogida { get; set; }
    public int UnidadesPorBulto { get; set; }
    public decimal LargoCm { get; set; }
    public decimal AnchoCm { get; set; }
    public decimal AltoCm { get; set; }
    public decimal PesoBultoKg { get; set; }
}
