namespace TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

public class ProductoVentaDto
{
    public int ProductoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int UnidadesVendidas { get; set; }

    public decimal MontoVendido { get; set; }
}
