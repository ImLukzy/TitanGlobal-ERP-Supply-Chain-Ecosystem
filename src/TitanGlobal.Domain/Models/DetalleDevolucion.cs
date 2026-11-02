namespace TitanGlobal.Domain.Models;

public class DetalleDevolucion
{
    public int Id { get; set; }

    public int DevolucionComercialId { get; set; }

    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public virtual DevolucionComercial DevolucionComercial { get; set; } = null!;

    public virtual Producto Producto { get; set; } = null!;
}
