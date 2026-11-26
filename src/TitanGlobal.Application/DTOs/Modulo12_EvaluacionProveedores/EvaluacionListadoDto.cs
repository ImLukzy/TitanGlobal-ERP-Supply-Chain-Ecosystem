namespace TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;

public class EvaluacionListadoDto
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public string Periodo { get; set; } = string.Empty;

    public decimal PuntajeFinal { get; set; }

    public string Calificacion { get; set; } = string.Empty;
}
