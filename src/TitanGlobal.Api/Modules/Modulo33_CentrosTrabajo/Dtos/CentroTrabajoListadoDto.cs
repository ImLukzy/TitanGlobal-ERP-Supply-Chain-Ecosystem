namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.Dtos;

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
