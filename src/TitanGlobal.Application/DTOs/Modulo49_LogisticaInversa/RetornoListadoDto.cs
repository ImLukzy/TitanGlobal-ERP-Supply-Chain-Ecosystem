namespace TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;

public class RetornoListadoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string ReferenciaOrigen { get; set; } = string.Empty;
    public string Contraparte { get; set; } = string.Empty;
    public string PuntoRecogida { get; set; } = string.Empty;
    public string PuntoRecepcion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Id { get; set; }
    public DateTime FechaProgramada { get; set; }
    public string CantidadProgramada { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
