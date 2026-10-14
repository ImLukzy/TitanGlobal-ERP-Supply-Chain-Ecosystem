namespace TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

public class ProductoDetalleDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public string? Descripcion { get; set; }

    public int UnidadesVendidas { get; set; }

    public decimal MontoVendido { get; set; }

    public int ClientesDistintos { get; set; }
}
