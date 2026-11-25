namespace TitanGlobal.Application.DTOs.Modulo66_Planillas;

public class ParametroPensionDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Sistema { get; set; } = string.Empty;

    public decimal TasaAporte { get; set; }

    public decimal TasaPrimaSeguro { get; set; }

    public decimal TasaComision { get; set; }

    public decimal? RemuneracionMaximaAsegurable { get; set; }
}
