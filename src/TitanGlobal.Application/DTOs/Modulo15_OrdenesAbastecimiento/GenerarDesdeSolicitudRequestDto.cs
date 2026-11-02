namespace TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;

public class GenerarDesdeSolicitudRequestDto
{
    public int? ProveedorId { get; set; }

    public DateTime FechaEntregaEstimada { get; set; }

    public string? Observaciones { get; set; }
}
