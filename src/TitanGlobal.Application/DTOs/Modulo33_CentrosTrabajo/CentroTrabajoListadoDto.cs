namespace TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;

public class CentroTrabajoListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Planta { get; set; } = string.Empty;

    public decimal CapacidadHorasDia { get; set; }

    public bool Activo { get; set; }
}
