namespace TitanGlobal.Application.DTOs.Modulo26_KardexValorizado;

public class PaginaMovimientosDto
{
    public List<MovimientoListadoDto> Items { get; set; } = new List<MovimientoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
