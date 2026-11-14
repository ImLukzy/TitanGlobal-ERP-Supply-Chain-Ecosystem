namespace TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;

public class PaginaFacturasDto
{
    public List<FacturaListadoDto> Items { get; set; } = new();

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }

    public int TotalRegistros { get; set; }

    public int TotalPaginas { get; set; }
}
