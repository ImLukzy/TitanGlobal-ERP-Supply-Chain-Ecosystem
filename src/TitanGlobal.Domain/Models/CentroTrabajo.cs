namespace TitanGlobal.Domain.Models;

public partial class CentroTrabajo
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Planta { get; set; } = string.Empty;

    public decimal CapacidadHorasDia { get; set; }

    public decimal CostoHora { get; set; }

    public string? Responsable { get; set; }

    public bool Activo { get; set; } = true;
}
