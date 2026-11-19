namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class ActualizarAcuerdoRequestDto
{
    public string? Descripcion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoMaximo { get; set; }
}
