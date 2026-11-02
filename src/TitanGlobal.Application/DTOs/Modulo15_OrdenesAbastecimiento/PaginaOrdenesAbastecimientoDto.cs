namespace TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;

public class PaginaOrdenesAbastecimientoDto
{
    public List<OrdenAbastecimientoListadoDto> Items { get; set; } = new List<OrdenAbastecimientoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
