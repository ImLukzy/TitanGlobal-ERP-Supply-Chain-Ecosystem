namespace TitanGlobal.Application.DTOs.Modulo01_Clientes;

public class ClienteDetalleDto
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int TotalOrdenes { get; set; }

    public decimal MontoTotal { get; set; }

    public DateTime? UltimaOrdenFecha { get; set; }
}
