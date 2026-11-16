namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class CrearLicitacionRequestDto
{
    public string Numero { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal PresupuestoReferencial { get; set; }

    public DateTime FechaCierre { get; set; }
}
