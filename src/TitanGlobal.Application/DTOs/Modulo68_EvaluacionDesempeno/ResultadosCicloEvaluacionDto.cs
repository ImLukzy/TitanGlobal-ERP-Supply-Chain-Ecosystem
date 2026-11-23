namespace TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;

public class ResultadosCicloEvaluacionDto
{
    public int Evaluadas { get; set; }

    public decimal PuntajePromedio { get; set; }

    public List<DistribucionPuntajeDto> Distribucion { get; set; } = new List<DistribucionPuntajeDto>();

    public List<PromedioCompetenciaDto> PorCompetencia { get; set; } = new List<PromedioCompetenciaDto>();
}
