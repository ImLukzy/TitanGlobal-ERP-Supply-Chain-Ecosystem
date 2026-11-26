namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class CursoListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public decimal Horas { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string? Proveedor { get; set; }

    public bool Activo { get; set; }
}
