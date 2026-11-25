namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class PaginaTransportistasTercerosDto
{
    public List<TransportistaTerceroListadoDto> Items { get; set; } = new List<TransportistaTerceroListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
