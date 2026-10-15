namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class LineaSolicitudDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioEstimado { get; set; }

    public decimal Subtotal { get; set; }
}
