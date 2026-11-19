namespace TitanGlobal.Application.DTOs.Modulo20_AcuerdosAbastecimiento;

public class LineaAcuerdoDto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public decimal PrecioAcordado { get; set; }

    public int CantidadComprometida { get; set; }

    public int CantidadConsumida { get; set; }

    public int Saldo { get; set; }
}
