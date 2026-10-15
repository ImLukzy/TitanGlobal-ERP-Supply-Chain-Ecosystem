namespace TitanGlobal.Api.Models;

public class Vendedor
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Email { get; set; }

    public decimal PorcentajeComision { get; set; }

    public bool Activo { get; set; }
}
