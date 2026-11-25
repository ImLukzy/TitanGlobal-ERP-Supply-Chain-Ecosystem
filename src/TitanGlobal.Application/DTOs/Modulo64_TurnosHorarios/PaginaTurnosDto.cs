namespace TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;

public class PaginaTurnosDto
{
    public List<TurnoListadoDto> Items { get; set; } = new List<TurnoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
