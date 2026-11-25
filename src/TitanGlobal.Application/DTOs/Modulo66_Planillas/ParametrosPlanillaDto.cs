namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class ParametrosPlanillaDto
{
    public List<ParametroPlanillaDto> Parametros { get; set; } = new List<ParametroPlanillaDto>();

    public List<ParametroPensionDto> Pensiones { get; set; } = new List<ParametroPensionDto>();

    public List<TramoQuintaDto> Tramos { get; set; } = new List<TramoQuintaDto>();
}
