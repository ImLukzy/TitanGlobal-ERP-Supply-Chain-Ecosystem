namespace TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;

public class PaginaEquiposProteccionDto
{
    public List<EquipoProteccionListadoDto> Items { get; set; } = new List<EquipoProteccionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
