namespace TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;

public class PaginaRutasDespachoDto
{
    public List<RutaDespachoListadoDto> Items { get; set; } = new List<RutaDespachoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
