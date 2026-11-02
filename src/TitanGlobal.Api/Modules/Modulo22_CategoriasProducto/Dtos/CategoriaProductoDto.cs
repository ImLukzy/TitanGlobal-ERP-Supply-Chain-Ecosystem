namespace TitanGlobal.Api.Modules.Modulo22_CategoriasProducto.Dtos;

public class CategoriaProductoDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activa { get; set; }
}
