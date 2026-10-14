namespace TitanGlobal.Api.Models;

public partial class OrdenCompra
{
    public int Id { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public string? Descripcion { get; set; }

    public int ClienteId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();
}
