namespace TitanGlobal.Application.DTOs.Modulo16_Importaciones;

public class PaginaExpedientesDto
{
    public List<ExpedienteListadoDto> Items { get; set; } = new List<ExpedienteListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
