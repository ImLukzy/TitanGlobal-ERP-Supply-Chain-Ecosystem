namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class CrearExpedienteRequestDto
{
    public string Numero { get; set; } = string.Empty;

    public int OrdenAbastecimientoId { get; set; }

    public string Incoterm { get; set; } = string.Empty;

    public string MedioTransporte { get; set; } = string.Empty;

    public string PaisOrigen { get; set; } = string.Empty;

    public string LugarOrigen { get; set; } = string.Empty;

    public string LugarDestino { get; set; } = string.Empty;

    public DateOnly FechaLlegadaEstimada { get; set; }

    public string? Observaciones { get; set; }
}
