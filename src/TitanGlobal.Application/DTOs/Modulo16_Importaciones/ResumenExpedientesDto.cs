namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class ResumenExpedientesDto
{
    public int TotalExpedientes { get; set; }

    public int EnCurso { get; set; }

    public List<EstadoConteoDto> PorEstado { get; set; } = new List<EstadoConteoDto>();
}
