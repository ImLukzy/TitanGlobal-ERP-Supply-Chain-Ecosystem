namespace TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;

public class DetalleOrdenItemDto
{
    public int ProductoId { get; set; }

    public string NombreProducto { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}
