namespace TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

public class ProductoListadoDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public bool TieneDescripcion { get; set; }
}
