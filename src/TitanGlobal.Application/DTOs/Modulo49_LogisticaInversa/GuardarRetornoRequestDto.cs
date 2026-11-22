namespace TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;

public class GuardarRetornoRequestDto
{
    public string Codigo { get; set; } = string.Empty;
    public string ReferenciaOrigen { get; set; } = string.Empty;
    public string Contraparte { get; set; } = string.Empty;
    public string PuntoRecogida { get; set; } = string.Empty;
    public string PuntoRecepcion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime? FechaProgramada { get; set; }
    public decimal CantidadProgramada { get; set; }
    public string? Observaciones { get; set; }
}
