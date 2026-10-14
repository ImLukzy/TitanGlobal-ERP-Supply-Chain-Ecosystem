namespace TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;

public class PaginaClientesDto
{
    public List<ClienteResumenDto> Items { get; set; } = new List<ClienteResumenDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
