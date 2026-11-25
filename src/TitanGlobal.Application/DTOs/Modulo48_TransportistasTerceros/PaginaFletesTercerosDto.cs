namespace TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;

public class PaginaFletesTercerosDto
{
    public List<FleteTerceroListadoDto> Items { get; set; } = new List<FleteTerceroListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
