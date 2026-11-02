namespace TitanGlobal.Application.DTOs.Modulo22_CategoriasProducto;

public class CategoriaProductoDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activa { get; set; }
}
