namespace TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;

public class LineaGuiaDto
{
    public int Id { get; set; }

    public int Posicion { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string UnidadMedida { get; set; } = string.Empty;

    public string Cantidad { get; set; } = string.Empty;
}
