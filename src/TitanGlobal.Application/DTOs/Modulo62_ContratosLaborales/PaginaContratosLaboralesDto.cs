namespace TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;

public class PaginaContratosLaboralesDto
{
    public List<ContratoLaboralListadoDto> Items { get; set; } = new List<ContratoLaboralListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
