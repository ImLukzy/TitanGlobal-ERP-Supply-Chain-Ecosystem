namespace TitanGlobal.Application.DTOs.Modulo45_GuiasRemision;

public class GuardarGuiaRequestDto
{
    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public DateTime? FechaInicioTraslado { get; set; }

    public string RemitenteNombre { get; set; } = string.Empty;

    public string RemitenteDocumento { get; set; } = string.Empty;

    public string DestinatarioNombre { get; set; } = string.Empty;

    public string DestinatarioDocumento { get; set; } = string.Empty;

    public string DireccionOrigen { get; set; } = string.Empty;

    public string DireccionDestino { get; set; } = string.Empty;

    public string MotivoTraslado { get; set; } = string.Empty;

    public string Transportista { get; set; } = string.Empty;

    public string Conductor { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public decimal PesoBrutoKg { get; set; }

    public string? Observaciones { get; set; }

    public List<LineaGuiaRequestDto>? Lineas { get; set; } = new();
}
