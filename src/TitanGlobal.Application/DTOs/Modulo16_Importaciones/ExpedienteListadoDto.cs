namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class ExpedienteListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string OrdenNumero { get; set; } = string.Empty;

    public string ProveedorNombre { get; set; } = string.Empty;

    public string Incoterm { get; set; } = string.Empty;

    public string MedioTransporte { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateOnly FechaLlegadaEstimada { get; set; }
}
