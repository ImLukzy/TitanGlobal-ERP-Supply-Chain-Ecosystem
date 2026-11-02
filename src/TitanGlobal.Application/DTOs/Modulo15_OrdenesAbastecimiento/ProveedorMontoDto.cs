namespace TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;

public class ProveedorMontoDto
{
    public int ProveedorId { get; set; }

    public string RazonSocial { get; set; } = string.Empty;

    public int Ordenes { get; set; }

    public decimal Monto { get; set; }
}
