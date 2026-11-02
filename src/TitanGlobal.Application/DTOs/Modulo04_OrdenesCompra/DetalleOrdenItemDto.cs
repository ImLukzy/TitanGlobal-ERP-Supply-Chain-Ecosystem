namespace TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;

public class DetalleOrdenItemDto
{
    public int ProductoId { get; set; }

    public string NombreProducto { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}
