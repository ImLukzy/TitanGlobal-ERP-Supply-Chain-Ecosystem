namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class ResumenSolicitudesDto
{
    public int Total { get; set; }

    public int Pendientes { get; set; }

    public int Aprobadas { get; set; }

    public int Rechazadas { get; set; }

    public decimal MontoPendiente { get; set; }

    public decimal MontoAprobado { get; set; }

    public List<ProductoSolicitadoDto> TopProductosSolicitados { get; set; } = new List<ProductoSolicitadoDto>();
}
