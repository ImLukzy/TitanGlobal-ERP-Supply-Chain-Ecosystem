namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class LineaSolicitudRequestDto
{
    public int ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioEstimado { get; set; }
}
