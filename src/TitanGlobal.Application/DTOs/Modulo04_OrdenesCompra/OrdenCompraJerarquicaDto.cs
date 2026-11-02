namespace TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;

public class OrdenCompraJerarquicaDto
{
    public int OrdenId { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string NombreCliente { get; set; } = string.Empty;

    public List<DetalleOrdenItemDto> Detalles { get; set; } = new List<DetalleOrdenItemDto>();
}
