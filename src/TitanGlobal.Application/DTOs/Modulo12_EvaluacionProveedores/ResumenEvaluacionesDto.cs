namespace TitanGlobal.Application.DTOs.Modulo12_EvaluacionProveedores;

public class ResumenEvaluacionesDto
{
    public int TotalEvaluaciones { get; set; }

    public int ProveedoresEvaluados { get; set; }

    public decimal PromedioGeneral { get; set; }

    public List<CalificacionConteoDto> PorCalificacion { get; set; } = new List<CalificacionConteoDto>();

    public List<RankingProveedorDto> Ranking { get; set; } = new List<RankingProveedorDto>();
}
