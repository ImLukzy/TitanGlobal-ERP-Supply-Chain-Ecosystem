namespace TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;

public class RankingProveedorDto
{
    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public int Evaluaciones { get; set; }

    public decimal Promedio { get; set; }

    public string UltimaCalificacion { get; set; } = string.Empty;
}
