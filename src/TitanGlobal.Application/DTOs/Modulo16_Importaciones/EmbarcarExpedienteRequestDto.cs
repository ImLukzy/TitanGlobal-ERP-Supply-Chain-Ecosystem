namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class EmbarcarExpedienteRequestDto
{
    public string DocumentoTransporte { get; set; } = string.Empty;

    public DateOnly FechaEmbarque { get; set; }
}
