namespace TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

public class CategoriaResumenDto
{
    public string Categoria { get; set; } = string.Empty;

    public int TotalProductos { get; set; }

    public decimal PrecioPromedio { get; set; }

    public decimal PrecioMaximo { get; set; }
}
