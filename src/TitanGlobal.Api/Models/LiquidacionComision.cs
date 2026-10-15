namespace TitanGlobal.Api.Models;

public class LiquidacionComision
{
    public int Id { get; set; }

    public int VendedorId { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public int Ordenes { get; set; }

    public decimal Ventas { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal Comision { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaCalculo { get; set; }

    public DateTime? FechaPago { get; set; }
}
