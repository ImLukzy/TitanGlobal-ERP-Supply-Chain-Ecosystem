namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class CrearLineaRequestDto
{
    public int ProductoId { get; set; }

    public decimal PrecioAcordado { get; set; }

    public int CantidadComprometida { get; set; }
}
