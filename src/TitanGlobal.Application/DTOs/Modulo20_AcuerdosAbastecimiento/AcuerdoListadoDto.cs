namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class AcuerdoListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string ProveedorNombre { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoMaximo { get; set; }

    public decimal MontoComprometido { get; set; }

    public decimal MontoConsumido { get; set; }
}
