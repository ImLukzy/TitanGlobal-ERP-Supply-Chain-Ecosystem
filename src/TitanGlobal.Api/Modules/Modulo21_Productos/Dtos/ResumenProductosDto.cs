namespace TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

public class ResumenProductosDto
{
    public int TotalProductos { get; set; }

    public int ProductosSinDescripcion { get; set; }

    public decimal PrecioPromedio { get; set; }

    public List<CategoriaResumenDto> PorCategoria { get; set; } = new List<CategoriaResumenDto>();

    public List<ProductoVentaDto> MasVendidos { get; set; } = new List<ProductoVentaDto>();
}
