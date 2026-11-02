namespace TitanGlobal.Application.DTOs.Modulo21_Productos;

public class PaginaProductosDto
{
    public List<ProductoListadoDto> Items { get; set; } = new List<ProductoListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
