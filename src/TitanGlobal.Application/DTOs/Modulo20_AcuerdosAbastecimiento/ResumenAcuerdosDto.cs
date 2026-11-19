namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class ResumenAcuerdosDto
{
    public int TotalAcuerdos { get; set; }

    public decimal MontoMaximoVigente { get; set; }

    public decimal MontoConsumido { get; set; }

    public List<EstadoConteoDto> PorEstado { get; set; } = new List<EstadoConteoDto>();
}
