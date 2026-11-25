namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class ExpedienteDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int OrdenAbastecimientoId { get; set; }

    public string OrdenNumero { get; set; } = string.Empty;

    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public string Incoterm { get; set; } = string.Empty;

    public string MedioTransporte { get; set; } = string.Empty;

    public string PaisOrigen { get; set; } = string.Empty;

    public string LugarOrigen { get; set; } = string.Empty;

    public string LugarDestino { get; set; } = string.Empty;

    public string? DocumentoTransporte { get; set; }

    public DateTime? FechaEmbarque { get; set; }

    public DateTime FechaLlegadaEstimada { get; set; }

    public DateTime? FechaLlegadaReal { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? MotivoCancelacion { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }
}
