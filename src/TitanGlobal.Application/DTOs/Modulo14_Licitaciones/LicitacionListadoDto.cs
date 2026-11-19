namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class LicitacionListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal PresupuestoReferencial { get; set; }

    public DateTime FechaCierre { get; set; }

    public int TotalOfertas { get; set; }
}
