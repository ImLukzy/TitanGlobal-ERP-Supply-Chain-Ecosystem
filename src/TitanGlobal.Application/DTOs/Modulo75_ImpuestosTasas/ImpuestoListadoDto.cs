namespace TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;

public class ImpuestoListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Pais { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public decimal? PorcentajeVigente { get; set; }

    public DateTime? FechaInicioVigencia { get; set; }
}
