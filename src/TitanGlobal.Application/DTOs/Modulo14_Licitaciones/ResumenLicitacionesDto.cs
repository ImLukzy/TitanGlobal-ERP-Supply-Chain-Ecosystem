namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class ResumenLicitacionesDto
{
    public int TotalLicitaciones { get; set; }

    public int TotalOfertas { get; set; }

    public decimal PresupuestoAbierto { get; set; }

    public decimal MontoAdjudicado { get; set; }

    public List<EstadoConteoDto> PorEstado { get; set; } = new List<EstadoConteoDto>();
}
