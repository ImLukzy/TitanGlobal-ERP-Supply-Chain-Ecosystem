namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class AcuerdoDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public decimal MontoMaximo { get; set; }

    public decimal MontoComprometido { get; set; }

    public decimal MontoConsumido { get; set; }

    public DateTime FechaRegistro { get; set; }

    public List<LineaAcuerdoDto> Lineas { get; set; } = new List<LineaAcuerdoDto>();
}
