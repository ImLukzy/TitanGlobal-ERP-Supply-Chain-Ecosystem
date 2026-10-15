namespace TitanGlobal.Api.Models;

public class AsignacionVenta
{
    public int Id { get; set; }

    public int OrdenCompraId { get; set; }

    public int VendedorId { get; set; }

    public DateTime FechaAsignacion { get; set; }
}
