namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class ActualizarLicitacionRequestDto
{
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public decimal PresupuestoReferencial { get; set; }

    public DateTime FechaCierre { get; set; }
}
