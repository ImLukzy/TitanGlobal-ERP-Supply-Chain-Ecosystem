namespace TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;

public class LineaSolicitudRequestDto
{
    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioEstimado { get; set; }
}
