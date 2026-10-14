namespace TitanGlobal.Api.Models;

public partial class Producto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Precio { get; set; }

    public string? Descripcion { get; set; }

    public virtual ICollection<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = new List<DetalleOrdenCompra>();
}
