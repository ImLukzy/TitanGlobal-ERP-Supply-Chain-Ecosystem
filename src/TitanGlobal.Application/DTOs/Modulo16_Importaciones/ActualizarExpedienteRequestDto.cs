namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class ActualizarExpedienteRequestDto
{
    public string Incoterm { get; set; } = string.Empty;

    public string MedioTransporte { get; set; } = string.Empty;

    public string PaisOrigen { get; set; } = string.Empty;

    public string LugarOrigen { get; set; } = string.Empty;

    public string LugarDestino { get; set; } = string.Empty;

    public DateOnly FechaLlegadaEstimada { get; set; }

    public string? Observaciones { get; set; }
}
