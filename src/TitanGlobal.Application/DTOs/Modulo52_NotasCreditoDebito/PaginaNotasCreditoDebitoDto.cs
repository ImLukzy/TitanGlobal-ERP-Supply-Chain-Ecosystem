namespace TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;

public sealed class PaginaNotasCreditoDebitoDto
{
    public List<NotaCreditoDebitoListadoDto> Items { get; set; } = new();
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
}
