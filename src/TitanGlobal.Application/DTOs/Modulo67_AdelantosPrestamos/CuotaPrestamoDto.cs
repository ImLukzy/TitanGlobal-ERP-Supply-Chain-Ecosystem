namespace TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;

public class CuotaPrestamoDto
{
    public int Id { get; set; }

    public int NumeroCuota { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    public decimal Monto { get; set; }

    public decimal MontoDescontado { get; set; }

    public string Estado { get; set; } = string.Empty;
}
