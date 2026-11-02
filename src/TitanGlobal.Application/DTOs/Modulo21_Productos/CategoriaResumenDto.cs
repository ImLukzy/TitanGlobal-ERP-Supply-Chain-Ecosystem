namespace TitanGlobal.Application.DTOs.Modulo21_Productos;

public class CategoriaResumenDto
{
    public string Categoria { get; set; } = string.Empty;

    public int TotalProductos { get; set; }

    public decimal PrecioPromedio { get; set; }

    public decimal PrecioMaximo { get; set; }
}
