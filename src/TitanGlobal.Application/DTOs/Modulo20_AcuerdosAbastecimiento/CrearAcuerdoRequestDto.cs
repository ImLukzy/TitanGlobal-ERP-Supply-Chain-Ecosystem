namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class CrearAcuerdoRequestDto
{
    public string Numero { get; set; } = string.Empty;

    public int ProveedorId { get; set; }

    public string? Descripcion { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoMaximo { get; set; }
}
