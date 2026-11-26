namespace TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;

public class LineaGuiaRequestDto
{
    public string Sku { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string UnidadMedida { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }
}
