namespace TitanGlobal.Api.Models;

public partial class DetalleSolicitudCompra
{
    public int Id { get; set; }

    public int SolicitudCompraId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioEstimado { get; set; }

    public virtual SolicitudCompra SolicitudCompra { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
