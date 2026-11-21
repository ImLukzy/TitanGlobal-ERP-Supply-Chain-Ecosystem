namespace TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;

public class GuiaListadoDto
{
    public int Id { get; set; }

    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaInicioTraslado { get; set; }

    public string DestinatarioNombre { get; set; } = string.Empty;

    public string DestinatarioDocumento { get; set; } = string.Empty;

    public string DireccionDestino { get; set; } = string.Empty;

    public string PesoBrutoKg { get; set; } = string.Empty;

    public int TotalLineas { get; set; }
}
