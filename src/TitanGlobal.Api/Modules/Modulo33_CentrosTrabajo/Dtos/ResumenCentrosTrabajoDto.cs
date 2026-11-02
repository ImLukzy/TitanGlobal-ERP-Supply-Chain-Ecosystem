namespace TitanGlobal.Api.Modules.Modulo33_CentrosTrabajo.Dtos;

public class ResumenCentrosTrabajoDto
{
    public int TotalCentros { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public decimal CapacidadTotalHorasDia { get; set; }

    public decimal CapacidadActivaHorasDia { get; set; }

    public List<TipoCentrosTrabajoDto> PorTipo { get; set; } = new List<TipoCentrosTrabajoDto>();

    public List<PlantaCentrosTrabajoDto> PorPlanta { get; set; } = new List<PlantaCentrosTrabajoDto>();
}
