namespace TitanGlobal.Api.Models;

public class ItemListaPrecios
{
    public int Id { get; set; }

    public int ListaPreciosId { get; set; }

    public int ProductoId { get; set; }

    public decimal Precio { get; set; }

    public virtual ListaPrecios ListaPrecios { get; set; } = null!;
}
