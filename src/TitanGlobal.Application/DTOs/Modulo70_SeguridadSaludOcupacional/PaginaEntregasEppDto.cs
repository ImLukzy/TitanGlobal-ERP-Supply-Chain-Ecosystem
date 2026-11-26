namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class PaginaEntregasEppDto
{
    public List<EntregaEppDto> Items { get; set; } = new List<EntregaEppDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
