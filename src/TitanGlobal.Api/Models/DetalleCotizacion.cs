namespace TitanGlobal.Api.Models;

public partial class DetalleCotizacion
{
    public int Id { get; set; }

    public int CotizacionId { get; set; }

    public virtual Cotizacion Cotizacion { get; set; } = null!;

    public int ProductoId { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal DescuentoPorcentaje { get; set; }
}
