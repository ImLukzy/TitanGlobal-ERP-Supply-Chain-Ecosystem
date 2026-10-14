namespace TitanGlobal.Api.Modules.Modulo21_Productos.Dtos;

public class GuardarProductoRequestDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public string? Descripcion { get; set; }
}
